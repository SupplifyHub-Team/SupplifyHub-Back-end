using API.DTO;
using API.DTO.Users;
using API.Factory;
using API.Services;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Text.RegularExpressions;
using YourNamespace;


namespace API
{
    [Route("api/")]
    [ApiController]
    public class RegisterController : APIBaseController
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmailService _emailService;
        public RegisterController(IUnitOfWork unitOfWork, IConfiguration config, ICloudinaryService cloudinary, IEmailService emailService) : base(unitOfWork, config)
        {
            _cloudinaryService = cloudinary;
            _emailService = emailService;
        }

        // --- RegisterSupplier ---
        /// <summary>
        /// **Function Summary:**
        /// This method handles the registration of a new user, which can be either a client or a supplier.
        /// It validates the user's input, hashes the password, and creates a new user entity.
        /// Depending on the account type, it performs different actions within a database transaction to ensure atomicity.
        /// For clients, it simply adds the user and assigns the 'Clients' role.
        /// For suppliers, it first validates and uploads a PDF file, then creates a supplier-specific profile
        /// with the uploaded file URL, assigns the 'Suppliers' role, and adds a default subscription plan and categories.
        /// After successful database operations, it sends a registration confirmation email to the user.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterSupplier([FromForm] UserRegisterDTO supplierRegisterDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            var newUser = new User
            {
                Name = supplierRegisterDTO.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(supplierRegisterDTO.password), // Ensure to hash the password in production
                Email = supplierRegisterDTO.email,
                CreatedAt = DateTime.UtcNow,
                Phone = supplierRegisterDTO.phoneNumber,
                EmailVerified = false,
                IsActive = true
            };
            if (supplierRegisterDTO.accountType == ClientType.Clients)
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    transaction = await _unitOfWork.BeginTransactionAsync();
                    await _unitOfWork.Users.AddAsync(newUser);
                    await _unitOfWork.SaveAsync();
                    var client = await _unitOfWork.Users.GetByEmailAsync(supplierRegisterDTO.email);
                    Role? clientRole = await _unitOfWork.Roles.GetRoleByRoleNameAsync(RoleName.Clients) ?? throw new Exception("Client role not found in database.");
                    await _unitOfWork.UserRoles.AddAsync(new()
                    {
                        UserId = client!.Id,
                        RoleId = clientRole!.Id
                    });
                    await _unitOfWork.SaveAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch 
                {
                    if (transaction != null)
                        await _unitOfWork.RollbackTransactionAsync();
                    return StatusCode(500, ResponseFactory.CreateMessageResponse("فشل الاشتراك"));
                }
            }
            else if (supplierRegisterDTO.accountType == ClientType.Suppliers)
            {
                // 7. Validate Pdf
                ValidateSuppliersProperty(supplierRegisterDTO);
                if (!ModelState.IsValid)
                    return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
                string pdfUrl;
                try
                {
                    pdfUrl = await _cloudinaryService.UploadPdfAsync(supplierRegisterDTO.textNumberPicture!);
                    if (string.IsNullOrEmpty(pdfUrl))
                        return BadRequest(ResponseFactory.CreateMessageResponse("PDF فشل رفع ال"));
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ResponseFactory.CreateMessageResponse($"PDF upload error: {ex.Message}"));
                }
                IDbContextTransaction transaction = null;
                try
                {
                    transaction = await _unitOfWork.BeginTransactionAsync();
                    await _unitOfWork.Users.AddAsync(newUser);
                    await _unitOfWork.SaveAsync();
                    var User = await _unitOfWork.Users.GetByEmailAsync(supplierRegisterDTO.email);
                    Supplier supplierCompany = new()
                    {
                        IsConfirmByAdmin = false,
                        TaxNumberURL = pdfUrl,
                        CountOfOrderAccepted = 0,
                        Description = "",
                        Locations = supplierRegisterDTO.locations!,
                        LogoURL = "https://res.cloudinary.com/dl2v9azqw/image/upload/v1757259229/112233_ciyjss.jpg",
                        UserId = User!.Id,
                        NumberOfViews = 0,
                        ImagePublicId = ""
                    };
                    await _unitOfWork.Suppliers.AddAsync(supplierCompany);
                    await _unitOfWork.SaveAsync();
                    Supplier supplier = await _unitOfWork.Suppliers.GetByUserIdAsync(User.Id);
                    SubscriptionPlan subscriptionPlan = _unitOfWork.SubscriptionPlans.GetById(4); //4
                    Role? role = await _unitOfWork.Roles.GetRoleByRoleNameAsync(RoleName.Suppliers) ?? throw new Exception("Client role not found in database.");
                    UserRole userRole = new() 
                    {
                        UserId = User!.Id,
                        RoleId = role!.Id
                    };
                    SupplierSubscriptionPlan plan = new() 
                    {
                        CompetitorAndMarketAnalysis = false,
                        ProductVisitsAndPerformanceAnalysis = false,
                        CreatedAt = DateTime.UtcNow,
                        DirectTechnicalSupport = false,
                        EarlyAccessToOrder = false,
                        EndDate = DateTime.UtcNow.AddMonths(subscriptionPlan.Duration),
                        NumberOfAcceptOrder = 30,
                        NumberOfAdvertisement = 0,
                        NumberOfProduct = 0,
                        NumberOfSpecialProduct = 0,
                        PaymentStatus = PaymentStatus.Completed,
                        PlanName = subscriptionPlan.Name,
                        ShowHigherInSearch = false,
                        StartDate = DateTime.UtcNow,
                        SupplierId = supplier.Id,
                        PlanId = subscriptionPlan.Id
                    };
                    List<SupplierCategory> supplierCategories = supplierRegisterDTO.categoriesId!.Select(c =>
                    new SupplierCategory()
                    {
                        SupplierId = supplier.Id,
                        CategoryId = c,
                    }).ToList();
                    await _unitOfWork.SupplierCategories.AddRangeAsync(supplierCategories);
                    await _unitOfWork.UserRoles.AddAsync(userRole);
                    await _unitOfWork.SupplierSubscriptionPlans.AddAsync(plan);
                    await _unitOfWork.SaveAsync();
                    await _unitOfWork.CommitTransactionAsync();
                    var EmailToTellSupplierToWaitUntilAdminConfirm = new EmailDto()
                    {
                        To = supplierRegisterDTO.email,
                        Subject = "طلب التسجيل قيد المراجعة - منصة B2B",
                        Body = $@"
                                <div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                                    <h2 style=""color: #007bff; text-align: center;"">مرحباً بك في منصة B2B</h2>
                                    <p><strong>عزيزي المورد،</strong></p>
                                    <p>شكراً جزيلاً لتسجيلك معنا. لقد استلمنا طلبك بنجاح ونود إخبارك أننا نقوم الآن بمراجعة جميع الملفات التي قمت بتحميلها.</p>
                                    <p>هذه العملية ضرورية لضمان دقة وسلامة البيانات على منصتنا.</p>
                                    <p>سنقوم بإرسال رسالة بريد إلكتروني أخرى فور الانتهاء من المراجعة وتأكيد حسابك. يرجى التحقق من بريدك الوارد بانتظام.</p>
                                    <p>نقدر صبرك وتفهمك ونتطلع للعمل معك قريباً.</p>
                                    <p>مع أطيب التحيات،</p>
                                    <p><strong>SuppliFy فريق</strong></p>
                                    <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                                    <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                                </div>
                            "
                    };
                    _emailService.SendEmail(EmailToTellSupplierToWaitUntilAdminConfirm);
                }
                catch 
                {
                    if (transaction != null)
                        await _unitOfWork.RollbackTransactionAsync();
                    return StatusCode(500, ResponseFactory.CreateMessageResponse("فشل الإشتراك"));
                }
            }
            string secreteToken = GenerateSecureToken(email: newUser.Email);
            var notificationEmail = new EmailDto()
            {
                To = supplierRegisterDTO.email,
                Subject = "تأكيد بريدك الإلكتروني - منصة SupplyFi",
                Body = $@"
                    <div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                        <h2 style=""color: #007bff; text-align: center;"">مرحباً بك في منصة B2B</h2>
                        <p><strong>عزيزي المورد،</strong></p>
                        <p>شكراً لتسجيلك معنا. يرجى تأكيد بريدك الإلكتروني بالضغط على الرابط أدناه لاستكمال عملية التسجيل.</p>
                        <p style=""text-align: center; margin: 30px 0;"">
                            <a href=""https://b2bapp.runasp.net/api/{secreteToken}"" style=""background-color: #007bff; color: #fff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;"">تأكيد البريد الإلكتروني</a>
                        </p>
                        <p>هذه الخطوة ضرورية لتفعيل حسابك.</p>
                        <p>مع أطيب التحيات،</p>
                        <p><strong>فريق منصة SuppliFy</strong></p>
                        <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                        <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                    </div>
            "
            };
            _emailService.SendEmail(notificationEmail);
            return Ok(ResponseFactory.CreateMessageResponse(supplierRegisterDTO.accountType == ClientType.Clients ? "تم أنشاء حسابك بنجاح تم أرسال أيميل لتأكيد البريد الكترونى الخاص بك": "تم أنشاء حسابك بنجاح تم أرسال أيميل لتأكيد البريد الكترونى الخاص بك و أنتظر تأكيد فريق الدعم لحسابك"));
        }

        // --- ConfirmEmail ---
        /// <summary>
        /// **Function Summary:**
        /// This method confirms a user's email address by validating a secure token.
        /// It receives a token from the URL, validates it, and checks if the corresponding user exists.
        /// If the token is valid and the user is found, it updates the user's `EmailVerified` status in the database to true.
        /// The method then returns a success HTML page to the user's browser, indicating that their email has been confirmed.
        /// If the token is invalid or the user is not found, it returns an error HTML page.
        /// </summary>
        [HttpGet("{secretToken}")]
        public async Task<IActionResult> ConfirmEmail([FromRoute] string secretToken)
        {
            string contentBody = @"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هذا الرابط غير صالح.</p>
                              </div>
                            </body>
                            </html>";
            if (!ValidateToken(secretToken, out string email))
                return Content(contentBody, "text/html");
            User user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
                return Content(contentBody ,"text/html");
            user.EmailVerified = true;
            _unitOfWork.Users.Update(user);
            _unitOfWork.Save();
            return Content("<!DOCTYPE html>\r\n<html lang=\"ar\" dir=\"rtl\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>تم تأكيد البريد الإلكتروني</title>\r\n    <link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">\r\n    <link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>\r\n    <link href=\"https://fonts.googleapis.com/css2?family=Tajawal:wght@400;700&display=swap\" rel=\"stylesheet\">\r\n    <style>\r\n        body {\r\n            font-family: 'Tajawal', sans-serif;\r\n            background-color: #f4f7f9;\r\n            display: flex;\r\n            justify-content: center;\r\n            align-items: center;\r\n            height: 100vh;\r\n            margin: 0;\r\n            text-align: center;\r\n        }\r\n        .container {\r\n            background-color: #ffffff;\r\n            padding: 40px 30px;\r\n            border-radius: 12px;\r\n            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);\r\n            max-width: 500px;\r\n            width: 90%;\r\n            direction: rtl;\r\n        }\r\n        .icon {\r\n            color: #28a745;\r\n            font-size: 80px;\r\n            margin-bottom: 20px;\r\n        }\r\n        h1 {\r\n            color: #007bff;\r\n            font-size: 28px;\r\n            margin-bottom: 15px;\r\n        }\r\n        p {\r\n            color: #555;\r\n            font-size: 18px;\r\n            line-height: 1.8;\r\n            margin-bottom: 25px;\r\n        }\r\n        a {\r\n            background-color: #007bff;\r\n            color: #ffffff;\r\n            padding: 12px 25px;\r\n            text-decoration: none;\r\n            border-radius: 8px;\r\n            font-weight: bold;\r\n            transition: background-color 0.3s ease;\r\n        }\r\n        a:hover {\r\n            background-color: #0056b3;\r\n        }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h1>تم تأكيد البريد الإلكتروني بنجاح!</h1>\r\n        <p>لقد تم تفعيل حسابك الآن. يمكنك العودة إلى التطبيق أو تسجيل الدخول لبدء استخدام المنصة.</p>\r\n        <a href=\"https://b2b-platform-nu.vercel.app/en/login\">الانتقال إلى المنصة</a>\r\n    </div>\r\n</body>\r\n</html>", "text/html");
        }

        // --- ReconfirmEmail ---
        /// <summary>
        /// **Function Summary:**
        /// This method handles the request to re-send a user's email confirmation link.
        /// It validates the provided email address and checks if a corresponding user exists.
        /// If a user is found, it generates a new secure token, constructs a new email confirmation message with a link containing the new token, and sends it to the user.
        /// For security purposes, it returns a confirmation message regardless of whether the email exists in the system to prevent revealing user information.
        /// </summary>
        [HttpPost("confirm-email/{email}")]
        public async Task<IActionResult> ReconfirmEmail([FromRoute]string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("الإيميل مطلوب"));
            }
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) // A simple regex for email validation
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("شكل الإيميل غير صحيح"));
            }
            else if (email.Length > 255)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("أقصى عدد من الحروف 255"));
            }
            User? user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user != null)
            {
                string secreteToken = GenerateSecureToken(email:email);
                var notificationEmail = new EmailDto()
                {
                    To = email,
                    Subject = "تأكيد بريدك الإلكتروني - منصة SupplyFi",
                    Body = $@"
                    <div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                        <h2 style=""color: #007bff; text-align: center;"">مرحباً بك في منصة B2B</h2>
                        <p><strong>عزيزي المورد،</strong></p>
                        <p>شكراً لتسجيلك معنا. يرجى تأكيد بريدك الإلكتروني بالضغط على الرابط أدناه لاستكمال عملية التسجيل.</p>
                        <p style=""text-align: center; margin: 30px 0;"">
                            <a href=""https://b2bapp.runasp.net/api/{secreteToken}"" style=""background-color: #007bff; color: #fff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;"">تأكيد البريد الإلكتروني</a>
                        </p>
                        <p>هذه الخطوة ضرورية لتفعيل حسابك.</p>
                        <p>مع أطيب التحيات،</p>
                        <p><strong>فريق منصة SuppliFy</strong></p>
                        <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                        <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                    </div>
                    "
                };
                _emailService.SendEmail(notificationEmail);
            }
            return Ok(ResponseFactory.CreateMessageResponse("تم ارسال إيميل التأكيد إذا كان هذا الإيميل موجود"));
        }

        // --- ValidateSuppliersProperty ---
        /// <summary>
        /// **Function Summary:**
        /// This private helper method validates supplier-specific properties required during registration.
        /// It checks if a documentation PDF file is provided, ensures its size is within the 5MB limit, and verifies its '.pdf' extension.
        /// It also validates that categories and locations are selected, and that each category ID exists in the database.
        /// Any validation errors found are added to the `ModelState` object.
        /// </summary>
        private void ValidateSuppliersProperty(UserRegisterDTO userRegisterDTO) 
        {
            var allowedPdfExtensions = new[] { ".pdf" };
            const long maxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB
            if (userRegisterDTO.textNumberPicture == null)
                ModelState.AddModelError("TextNumberPicture", "ملف التوثيق مطلوب");
            else
            {
                var textNumberFileExtension = Path.GetExtension(userRegisterDTO.textNumberPicture.FileName).ToLowerInvariant();
                if (userRegisterDTO.textNumberPicture.Length > maxFileSizeInBytes)
                    ModelState.AddModelError("TextNumberPicture", " 5 MB حجم الملف يجب أن يكون أقل من .");
                else if (!allowedPdfExtensions.Contains(textNumberFileExtension))
                    ModelState.AddModelError("TextNumberPicture", " فقط .pdf ملفات بامتداد");
            }

            if (userRegisterDTO.categoriesId == null || !userRegisterDTO.categoriesId.Any())
                ModelState.AddModelError("CategoriesId", "الفئة مطلوبة");
            else
            {
                foreach (var catId in userRegisterDTO.categoriesId)
                {
                    if (_unitOfWork.Categories.GetById(catId) == null)
                    {
                        ModelState.AddModelError("CategoriesId", $"this id: {catId} الفئة غير موجودة");
                        break;
                    }
                }
            }
            if (userRegisterDTO.locations == null || userRegisterDTO.locations.Count == 0)
                ModelState.AddModelError("Locations", "يجب إضافة موقع");
        }

    }
}
