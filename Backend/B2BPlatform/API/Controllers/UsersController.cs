using API.DTO;
using API.DTO.GeneralResponse;
using API.DTO.Users;
using API.Factory;
using API.Services;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace API.Controllers
{

    [Route("api/")]
    [ApiController]
    public class UsersController : APIBaseController
    {
        private readonly IEmailService _emailService;
        public UsersController(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork)
        {
            _emailService = emailService;
        }
        /// <summary>
        /// **Function Summary:**  
        /// This method handles the "Contact Us" requests submitted by users.  
        /// It accepts a `ContactUsDto` object containing the user's name, email, and message text.  
        /// The method then constructs an `EmailDto` and sends it to the system's support email address.  
        /// Returns `200 OK` with "Created!" if the email is sent successfully.  
        /// Returns `200 OK` with an error message if an exception occurs.  
        /// </summary>
        [HttpPost("contact")]
        public async Task<IActionResult> ContactUs([FromBody] ContactUsDto contactUsDto)
        {

            try
            {
                var Email = new EmailDto()
                {
                    Subject = "Email from a user",
                    Body = $"client name: {contactUsDto.Name},<br><br>Cleint Email: {contactUsDto.Email},<br><br> say that: {contactUsDto.QueryText} ",
                    To = "supplifyteam@gmail.com"
                };
                _emailService.SendEmail(Email);
                return Ok("Created!");
            }
            catch (Exception ex)
            {
                return Ok("Something wrong happend");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of users with optional filters including search text, role,
        /// category, location, and account status.  
        /// Validates the provided role, category, and sort direction before building the query.  
        /// Excludes admin users from the results.  
        /// Returns user details such as name, role, categories, join date, locations, email, and phone.  
        /// Supports custom sorting and pagination.  
        /// </summary>
        /// <param name="search">Optional text to search by user name or email.</param>
        /// <param name="role">Optional role filter; must be a valid RoleName enum.</param>
        /// <param name="category">Optional category filter; must exist in the database.</param>
        /// <param name="location">Optional location filter; matches supplier locations.</param>
        /// <param name="isActive">Optional status filter; true for active, false for inactive.</param>
        /// <param name="page">The page number for pagination (default is 1).</param>
        /// <param name="pageSize">The number of records per page (default is 10).</param>
        /// <param name="sortColumn">Column to sort by (default is CreatedAt).</param>
        /// <param name="sortColumnDirection">Sorting direction, Asc or Desc (default is Desc).</param>
        /// <returns>
        /// A paginated response containing a filtered list of users and metadata (total count, pages, etc.).  
        /// </returns>
        [Authorize(Roles ="Admin")]
        [HttpGet("admin/users")]
        public async Task<IActionResult> GetAllUsersWithFilters(
            string? search,
            string? role,
            int? category,
            string? location,
            bool? isActive,
            int page = 1,
            int pageSize = 10,
             string sortColumn = "CreatedAt",
             string sortColumnDirection = "Desc")
        {
            // 1. Validate role if provided
            RoleName parsedRole = default;
            bool roleIsValid = true;
            if (!string.IsNullOrEmpty(role))
                roleIsValid = System.Enum.TryParse<RoleName>(role, true, out parsedRole);

            // 2. Validate category if provided
            bool categoryIsValid = true;
            if (category != null)
                categoryIsValid = (await _unitOfWork.Categories.FindAsync(c => c.Id == category)) != null;

            // 3. If any provided filter is invalid, return no users
            if (!roleIsValid || !categoryIsValid)
                return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize,page));
            OrderBy orderBy = default;
            bool statusIsValid = true;
            if (!string.IsNullOrEmpty(sortColumnDirection))
                statusIsValid = System.Enum.TryParse<OrderBy>(sortColumnDirection, true, out orderBy);
            if (!statusIsValid)
                return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize,page));

            Expression<Func<User, bool>> criteria = u =>
                (string.IsNullOrEmpty(search) || u.Name.ToLower().Contains(search.ToLower()) || u.Email.Contains(search)) &&
                (!isActive.HasValue || u.IsActive == isActive.Value) &&
                (string.IsNullOrEmpty(role) || u.UserRoles.Any(ur => ur.Role.Name == parsedRole)) &&
                (u.UserRoles.Any(ur => ur.Role.Name != RoleName.Admin))&&
                (string.IsNullOrEmpty(location) || u.Supplier.Locations.Any(loc => EF.Functions.ILike(loc, $"%{location}%"))) &&
                (category == null ||
                    (u.JopSeeker != null && u.JopSeeker.JopSeekerCategoryApplies.Any(ica => ica.Category.Id == category)) ||
                    (u.Supplier != null && u.Supplier.SupplierCategories.Any(cc => cc.Category.Id == category))
                );
            int totalCount = await _unitOfWork.Users.CountAsync(criteria);
            // 4. Build the query
            var users = await _unitOfWork.Users.FindWithFiltersAsync(
                1,
                criteria: criteria,
                sortColumn: sortColumn,
                sortColumnDirection: orderBy.ToString(),
                skip: (page - 1) * pageSize,
                take: pageSize
            );

            var result = users.Select(u => new AllUsersDto
            {
                Id = u.Id,
                Name = u.Name,
                UserName = u.Name,   /// will remove
                Role = u.UserRoles.Any() ? u.UserRoles.First().Role.Name.ToString() : "No Role",
                IsActive = u.IsActive,
                CategoryNames = u.JopSeeker != null
                    ? u.JopSeeker.JopSeekerCategoryApplies.Select(ica => ica.Category.Name).ToList()
                    : u.Supplier != null
                        ? u.Supplier.SupplierCategories.Select(cc => cc.Category.Name).ToList()
                        : new List<string>(),
                JoinDate = u.CreatedAt,
                Locations = u.Supplier != null ? u.Supplier.Locations : new List<string>(),
                Email = u.Email,
                PhoneNumber = u.Phone
            }).ToList();



            return Ok(ResponseFactory.CreatePaginationResponse(result,new Meta 
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize), // Fixed
                TotalItems = totalCount
            }));
        }

        /// <summary>
        /// Bans a user account by setting its <c>IsActive</c> flag to false.  
        /// Validates the provided user ID and ensures the user exists and is currently active.  
        /// Deletes all active tokens associated with the user to immediately revoke access.  
        /// Updates the user record in the database and saves the changes.  
        /// Returns an appropriate success or failure response message.  
        /// </summary>
        /// <param name="userId">The unique identifier of the user to be banned (must be positive).</param>
        /// <returns>HTTP response indicating success, failure, or invalid conditions.</returns>
        [Authorize(Roles ="Admin")]
        [HttpPatch("admin/ban")]
        public async Task<IActionResult> BanUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("   يجب أن يكون موجبا user id "));
            User user = await _unitOfWork.Users.FindAsync(u=> u.Id == userId);
            if(user is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح user id"));
            if(user.IsActive == false)
                return Ok(ResponseFactory.CreateMessageResponse("حساب مغلق بالفعل"));
            user.IsActive = false;
            var userTokens  = await _unitOfWork.UserTokens.FindAllAsync(ut => ut.UserId == userId);
            _unitOfWork.UserTokens.DeleteRange(userTokens);
            _unitOfWork.Users.Update(user);
            if( await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل في إيقاف حساب المستخدم حاول مرة أخرى"));
            return Ok(ResponseFactory.CreateMessageResponse("تم إيقاف حساب المستخدم"));
        }
        /// <summary>
        /// Activates a previously banned or inactive user account by setting its <c>IsActive</c> flag to true.  
        /// Validates the provided user ID and ensures the user exists and is currently inactive.  
        /// Updates the user record in the database and commits the changes.  
        /// Returns a success message if the operation is completed, or an error message if it fails.  
        /// </summary>
        /// <param name="userId">The unique identifier of the user to be activated (must be positive).</param>
        /// <returns>HTTP response indicating success, failure, or invalid conditions.</returns>
        [Authorize(Roles ="Admin")]
        [HttpPatch("admin/active")]
        public async Task<IActionResult> ActiveUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("   يجب أن يكون موجبا user id "));
            User user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح user id"));
            if (user.IsActive == true)
                return Ok(ResponseFactory.CreateMessageResponse("حساب مفعل بالفعل"));
            user.IsActive = true;
            _unitOfWork.Users.Update(user);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل تفعيل الحساب"));
            return Ok(ResponseFactory.CreateMessageResponse("تم تفعيل الحساب"));
        }

    }

}