using API.DTO;
using API.DTO.Users;
using API.Factory;
using API.Services;
using Controllers;
using Entities;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
[ApiController] 
[Route("api/")] 
public class AuthController : APIBaseController 
{
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly TokenService _tokenService;
    public AuthController(TokenService tokenService,IUnitOfWork unitOfWork,IConfiguration configuration, IEmailService emailService) : base(unitOfWork) 
    {
        this._configuration = configuration;
        this._emailService = emailService;
        this._tokenService = tokenService;
    }
    /// <summary>
    /// **Function Summary:**
    /// This method handles a user's initial login request. It validates the provided credentials
    /// (username and password) and, if successful, generates both a short-lived Access Token
    /// and a long-lived Refresh Token. The Access Token is returned in the response body,
    /// while the Refresh Token is set as an `HttpOnly` cookie for secure storage.
    /// The Refresh Token is also saved to the database for revocation purposes.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var user = await _unitOfWork.Users.GetByEmailIncludeRolesAsync(request.Email);
        if (user == null|| !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest(ResponseFactory.CreateMessageResponse("الإيميل أو الباسوورد غير صالح")); 
        if(!user.EmailVerified)
            return BadRequest(ResponseFactory.CreateMessageResponse("أكد الإيميل أولا لكى تستطيع فتح الحساب"));
        if (!user.IsActive)
            return BadRequest(ResponseFactory.CreateMessageResponse("الأكونت مغلق من قبل الأدمن"));
        if (user.Supplier is not null)
        {
            if(!user.Supplier.IsConfirmByAdmin)
                return BadRequest(ResponseFactory.CreateMessageResponse("أنتظر حتى يتم تأكيد الحساب من خلال الأدمن"));
        }
        var accessToken = await _unitOfWork.Users.GenerateToken(user,5,_configuration);
        var refreshTokenString = await GenerateRefreshToken(user);
        Response.Cookies.Append("refreshToken", refreshTokenString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, 
            SameSite = SameSiteMode.None, // Or SameSiteMode.Strict for stronger CSRF protection
            Expires = DateTime.UtcNow.AddDays(30), 
        });
        return Ok(ResponseFactory.CreateLoginResponse(accessToken,new UserDataDTO() 
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.UserRoles.First().Role.Name
        }));
    }

    // --- Method 2: Refresh ---
    /// <summary>
    /// **Function Summary:**
    /// This method allows a client to obtain a new Access Token when their current one expires,
    /// by presenting a valid Refresh Token. It retrieves the Refresh Token from an `HttpOnly` cookie,
    /// validates it against the database, and if valid, issues a new Access Token and a new Refresh Token
    /// (implementing refresh token rotation). The old Refresh Token is revoked in the database.
    /// </summary>
    // IMPORTANT: For robust CSRF protection, consider implementing Anti-CSRF tokens for this endpoint
    // in addition to the SameSite cookie attribute.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {

        var combinedTokenString = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(combinedTokenString))
            return Unauthorized(ResponseFactory.CreateMessageResponse("Refresh token not found."));
        var parts = combinedTokenString.Split(':');
        if (parts.Length != 2)
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(ResponseFactory.CreateMessageResponse("Invalid refresh token format."));
        }

        var tokenId = parts[0];
        var refreshTokenString = parts[1];

        var userToken = await _unitOfWork.UserTokens.FindAsync(ut => ut.Id == new Guid(tokenId));
        if (userToken == null)
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(ResponseFactory.CreateMessageResponse("Invalid or revoked refresh token."));
        }
        if (userToken.ExpiresAt <= DateTime.UtcNow)
        {
            _unitOfWork.UserTokens.Delete(userToken);
            await _unitOfWork.SaveAsync();

            Response.Cookies.Delete("refreshToken");
            return Unauthorized(ResponseFactory.CreateMessageResponse("Refresh token has expired. Please log in again."));
        }
        //_unitOfWork.UserTokens.Delete(userToken);
        var user = await _unitOfWork.Users.GetByIdAsync(userToken.UserId);
        if (user == null)
            return Unauthorized(ResponseFactory.CreateMessageResponse("User not found."));
        var newAccessToken =  await _unitOfWork.UserTokens.GenerateToken(user,5,_configuration);
        //var newRefreshToken = await GenerateRefreshToken(user);

        //Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
        //{
        //    HttpOnly = true,
        //    Secure = true,
        //    SameSite = SameSiteMode.None,
        //    Expires = DateTime.UtcNow.AddDays(30)
        //});
        return Ok(new { token = newAccessToken });
    }
    private async Task<string> GenerateRefreshToken(User user)
    {
        var tokenId = Guid.NewGuid();

        var refreshTokenString = $"{Guid.NewGuid().ToString()}{Guid.NewGuid().ToString()}";

        var hashedToken = BCrypt.Net.BCrypt.HashPassword(refreshTokenString);

        var newUserTokenToSave = new UserToken
        {
            Id = tokenId,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Set a long expiration time, e.g., 30 days.
            IsRevoked = false,
            Token = hashedToken,
            UserId = user.Id,
        };

        await _unitOfWork.UserTokens.AddAsync(newUserTokenToSave);
        await _unitOfWork.SaveAsync();

        return $"{tokenId}:{refreshTokenString}";        
    }

    // --- Method 3: Logout ---
    /// <summary>
    /// **Function Summary:**
    /// This method allows an authenticated user to log out. It revokes the user's Refresh Token
    /// in the database and clears the Refresh Token cookie from the browser, effectively ending
    /// the user's session. It requires a valid Access Token to be called.
    /// </summary>
    [Authorize] 
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest(ResponseFactory.CreateMessageResponse("User ID not found in token.")); 
       var userToken = await _unitOfWork.UserTokens.FindAllAsync(ut=> ut.UserId == userId);
       _unitOfWork.UserTokens.DeleteRange(userToken);
       if(await _unitOfWork.SaveAsync() == 0)
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في تسجل الخروج"));
       Response.Cookies.Delete("refreshToken");
       return Ok(ResponseFactory.CreateMessageResponse("تم تسجيل الخروج بنجاح")); 
    }

    // --- SendEmailToResetPassword ---
    /// <summary>
    /// **Function Summary:**
    /// This method handles the request to send a password reset email to a user.
    /// It first validates the provided email address to ensure it is not empty and follows a valid format.
    /// It then retrieves the user from the database. If a valid user is found, it generates a secure,
    /// time-limited token and creates an email containing a password reset link with this token.
    /// Finally, it sends the email and returns a confirmation message to the user,
    /// without revealing whether the email address exists in the system for security reasons.
    /// </summary>
    [HttpGet("reset-password/{email}")]
    public async Task<IActionResult> SendEmailToResetPassword([FromRoute] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("الإيميل مطلوب");
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return BadRequest("إيميل غير صالح");

        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user != null)
        {
            var token = _tokenService.GenerateSecureToken(
                orderId: 0,
                userId: user.Id,
                expiresAt: DateTime.UtcNow.AddHours(1)
            );
            var notificationEmail = new EmailDto()
            {
                To = email,
                Subject = "إعادة تعيين كلمة المرور - منصة B2B",
                Body = $@"
                        <div style=""font-family: 'Tajawal', sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                            <h2 style=""color: #007bff; text-align: center;"">طلب إعادة تعيين كلمة المرور</h2>
                            <p><strong>مرحباً،</strong></p>
                            <p>لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك. إذا لم تكن أنت من طلب ذلك، يمكنك تجاهل هذه الرسالة.</p>
                            <p>لإعادة تعيين كلمة المرور، يرجى الضغط على الرابط أدناه:</p>
                            <p style=""text-align: center; margin: 30px 0;"">
                                <a href=""https://b2b-platform-nu.vercel.app/reset-password?token={token}"" style=""background-color: #dc3545; color: #fff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">إعادة تعيين كلمة المرور</a>
                            </p>
                            <p>هذا الرابط صالح لمدة قصيرة. إذا انتهت صلاحيته، يمكنك طلب رابط جديد.</p>
                            <p>بالتوفيق،</p>
                            <p><strong>فريق منصة SuppliFy</strong></p>
                            <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                            <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                        </div>
                    "
            };
            _emailService.SendEmail(notificationEmail);
        }

        return Ok("تم إرسال إيميل إذا كان هذا الإيميل صالح");
    }

    // --- ResetPassword ---
    /// <summary>
    /// **Function Summary:**
    /// This method handles the password reset process after a user clicks the link in the reset email.
    /// It receives a token and a new password, performs validation on both, and then
    /// validates and parses the provided token to ensure its authenticity and expiration.
    /// It retrieves the user associated with the token, hashes the new password, and
    /// updates the user's password in the database. For security, it deletes all existing
    /// user tokens to prevent reuse. Finally, it sends a confirmation email to the user
    /// and returns a success response.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            ModelState.AddModelError("Token", "التوكن مطلوب");
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            ModelState.AddModelError("Password", "الباسوورد مطلوب ");
        else if (request.NewPassword.Length < 8)
            ModelState.AddModelError("Password", "يجب ألا يقل طول الباسوورد عن 8");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        var tokenData = _tokenService.ValidateAndParseToken(request.Token);
        if (tokenData == null)
            return BadRequest(ResponseFactory.CreateMessageResponse("انتهت مدة التوكن أو تم استخدامه"));


        var user = await _unitOfWork.Users.FindAsync(u=> u.Id == tokenData.UserId, ["UserTokens"]);
        if (user == null)
            return BadRequest(ResponseFactory.CreateMessageResponse("مستخدم غير موجود"));


        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        _unitOfWork.UserTokens.DeleteRange(user.UserTokens);
        _unitOfWork.Users.Update(user);
        if (await _unitOfWork.SaveAsync() == 0)
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في تغيير الباسوورد حاول مرة أخرى"));

        var notificationEmail = new EmailDto()
        {
            To = user.Email,
            Subject = "تم تغيير كلمة المرور بنجاح - منصة SupplyFi",
            Body = $@"
                    <div style=""font-family: 'Tajawal', sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                        <h2 style=""color: #28a745; text-align: center;"">تم تغيير كلمة المرور بنجاح</h2>
                        <p><strong>مرحباً،</strong></p>
                        <p>لقد تم تغيير كلمة المرور الخاصة بحسابك في منصة SupplyFi بنجاح. يمكنك الآن استخدام كلمة المرور الجديدة لتسجيل الدخول.</p>
                        <p style=""text-align: center; margin: 30px 0;"">
                            <a href=""https://b2b-platform-nu.vercel.app/en/login"" style=""background-color: #007bff; color: #fff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">تسجيل الدخول الآن</a>
                        </p>
                        <p>إذا لم تكن أنت من قام بهذا الإجراء، يرجى التواصل مع فريق الدعم فوراً.</p>
                        <p>بالتوفيق،</p>
                        <p><strong>فريق منصة SuppliFy</strong></p>
                        <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                        <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                    </div>
                    "
        };

        _emailService.SendEmail(notificationEmail);

        return Ok(ResponseFactory.CreateMessageResponse("تم تغيير كلمة المرور بنجاح"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/protected-data")] 
    public IActionResult GetProtectedData()
    {   
        var username = User.Identity.Name;
        return Ok(ResponseFactory.CreateMessageResponse($"Hello, {username}! This is protected data."));
    }
    
}

