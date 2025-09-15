using API.DTO;
using Microsoft.Extensions.Caching.Memory;
using API.DTO.GeneralResponse;
using API.DTO.Suppliers;
using API.DTO.Users;
using API.Factory;
using API.Services;
using Controllers;
using DAL;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Models.Entities;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Linq.Expressions;
using System.Security.Claims;
using YourNamespace;

namespace API.Controllers
{

    [Route("api/")]
    [ApiController]
    public class SuppliersController : APIBaseController
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmailService _emailService;

        public SuppliersController(IUnitOfWork unitOfWork, IConfiguration configuration , ICloudinaryService cloudinary, IEmailService emailService) : base(unitOfWork, configuration) 
        {
            this._cloudinaryService = cloudinary;
            this._emailService = emailService;
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves a paginated, filtered, and sorted list of all subscribed suppliers.  
        /// It is restricted to administrators only.  
        /// The endpoint supports filtering by supplier name, subscription plan name,  
        /// and payment status, with validation on input parameters.  
        /// It excludes invalid values by returning an empty pagination response.  
        /// Data is queried dynamically using an expression filter on users with supplier roles.  
        /// Each supplier record includes subscription details, plan dates, payment status,  
        /// and performance metrics like completed orders.  
        /// Results are returned as a list of `SubscribedSupplierDto` objects with metadata.  
        /// The final response is wrapped in a standardized pagination format.  
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/subscribed-suppliers")]
        public async Task<IActionResult> GetAllSubscribedSuppliers(
        string? search,
        string? planName,
        string? status,
        int page = 1,
        int pageSize = 10,
        string sortColumn = "CreatedAt",
        string sortColumnDirection = "Desc")
        {
            PaymentStatus paymentStatus = default;
            bool paymentStatusIsValid = true;
            if (!string.IsNullOrEmpty(status))
                paymentStatusIsValid = System.Enum.TryParse<PaymentStatus>(status, true, out paymentStatus);

            if (!paymentStatusIsValid)
                return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize, page));
            OrderBy orderBy = default;
            bool orderStatusIsValid = true;
            if (!string.IsNullOrEmpty(sortColumnDirection))
                orderStatusIsValid = System.Enum.TryParse<OrderBy>(sortColumnDirection, true, out orderBy);
            if (!orderStatusIsValid)
                return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize, page));
            
            Expression<Func<User, bool>> criteria = user =>
                user.Supplier != null &&
                user.Supplier.SupplierSubscriptionPlan!=null &&
                (string.IsNullOrEmpty(search) ||
                    user.Name.ToLower().Contains(search.ToLower())) &&
                (string.IsNullOrEmpty(planName) ||
                user.Supplier.SupplierSubscriptionPlan.PlanName == planName) &&
                (string.IsNullOrEmpty(status) ||
            user.Supplier.SupplierSubscriptionPlan.PaymentStatus == paymentStatus);
            int totalCount = await _unitOfWork.Users.CountAsync(criteria);

            var suppliers = await _unitOfWork.Users.FindWithFiltersAsync(
                3,
                criteria: criteria,
                sortColumn: sortColumn,
                sortColumnDirection: sortColumnDirection,
                skip: (page - 1) * pageSize,
                take: pageSize
            );

            var result = suppliers.Select(supplier =>
            {
                var subscription = supplier.Supplier.SupplierSubscriptionPlan;

                return new SubscribedSupplierDto
                {
                    UserId = supplier.Id,
                    CompanyName = supplier.Name,
                    Email = supplier.Email,
                    PlanName = subscription?.SubscriptionPlan?.Name,
                    PaymentStatus = subscription?.PaymentStatus.ToString(),
                    StartJoinPlanDate = subscription?.StartDate,
                    EndPlanDate = subscription?.EndDate,
                    JoinDate = supplier.CreatedAt,
                    OrdersCompleted = supplier.Supplier.Deals.Count(d => d.Status == DealStatus.AdminConfirmed)
                };
            }).ToList();
            return Ok(ResponseFactory.CreatePaginationResponse(result, new Meta
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalItems = totalCount
            }));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves a paginated, filtered, and sorted list of confirmed suppliers.  
        /// It supports optional filtering by search term (supplier name), location, and category.  
        /// If a category filter is provided, it validates the category before applying the filter.  
        /// The query ensures that only suppliers approved by an admin are included.  
        /// Suppliers are retrieved with their related subscription plans, locations, and categories.  
        /// Each supplier is mapped to a `SupplierForHomePageDto` with key details such as  
        /// company name, email, phone number, logo, categories, and subscription plan name.  
        /// Pagination metadata is included to handle large datasets efficiently.  
        /// Results are returned in a standardized paginated response format.  
        /// This helps clients search, filter, and browse suppliers effectively.  
        /// </summary>
        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers(
        string? search,
        string? location,
        int? category,
        int page = 1,
        int pageSize = 10,
        string sortColumn = "CreatedAt",
        string sortColumnDirection = "Desc")
        {
            // Validate category only if provided
            if (category.HasValue)
            {
                bool categoryIsValid = (await _unitOfWork.Categories.FindAsync(c => c.Id == category)) != null;
                if (!categoryIsValid)
                    return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize, page));
            }

            // Build criteria with conditional filters
            Expression<Func<User, bool>> criteria = user =>
                user.Supplier != null &&
                (string.IsNullOrEmpty(location) ||
                 user.Supplier.Locations.Any(loc => EF.Functions.ILike(loc, $"%{location}%"))) &&
                (string.IsNullOrEmpty(search) ||
                 user.Name.ToLower().Contains(search.ToLower())) &&
                 (user.Supplier.IsConfirmByAdmin) && // Only confirmed suppliers
                 (user.IsActive) && 
                 (user.EmailVerified) &&
                (!category.HasValue ||  // Only apply category filter if provided
                 user.Supplier.SupplierCategories.Any(cc => cc.Category.Id == category));

            int totalCount = await _unitOfWork.Users.CountAsync(criteria);

            var users = await _unitOfWork.Users.FindWithFiltersAsync(
                3,
                criteria: criteria,
                sortColumn: sortColumn,  // Use parameter instead of hardcoded
                sortColumnDirection: sortColumnDirection,
                skip: (page - 1) * pageSize,
                take: pageSize
            );

            var result = users.Select(user =>
            {
                var subscription = user.Supplier.SupplierSubscriptionPlan;

                return new SupplierForHomePageDto
                {
                    Id = user.Id,
                    CompanyName = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.Phone,
                    LogoUrl = user.Supplier?.LogoURL ?? string.Empty,
                    Locations = user.Supplier?.Locations ?? new List<string>(),
                    JoinedAt = user.CreatedAt,
                    CategoryNames = user.Supplier?.SupplierCategories
                        .Select(cc => cc.Category.Name)
                        .ToList() ?? new List<string>(),
                    PlanName = subscription?.SubscriptionPlan?.Name ?? "Not Subscribed"
                };
            }).ToList();

            return Ok(ResponseFactory.CreatePaginationResponse(result, new Meta
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalItems = totalCount
            }));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves all suppliers who are pending admin approval.  
        /// It is restricted to administrators through role-based authorization.  
        /// The query fetches suppliers not yet confirmed by an admin,  
        /// including related user data and supplier categories.  
        /// Each supplier is mapped into a `SuppliersToShowForAdminDto` object  
        /// with details like name, email, phone, created date, tax number, logo, and categories.  
        /// This provides admins with the necessary information to review suppliers.  
        /// Returns an `Ok` response with the list of suppliers awaiting approval.  
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/suppliers-to-accept")]
        public async Task<IActionResult> GetSuppliersToAccept()
        {
            var suppliers = await _unitOfWork.Suppliers.FindAllAsync(
                s => s.IsConfirmByAdmin == false,
                new[] { "User", "SupplierCategories.Category" }
            );
            var suppliersToShowForAdminDto =
                suppliers.Select(s => new SuppliersToShowForAdminDto
                {
                    Id = s.Id,
                    Name = s.User.Name,
                    Email = s.User.Email,
                    Phone = s.User.Phone,
                    CreatedAt = s.User.CreatedAt,
                    TaxNumberURL = s.TaxNumberURL,
                    LogoURL = s.LogoURL,
                    Categories = s.SupplierCategories
                        .Where(sc => sc.Category != null)
                        .Select(sc => sc.Category.Name)
                        .ToList()
                }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(suppliersToShowForAdminDto));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method allows an Admin to accept a supplier’s registration request.  
        /// It first validates the supplier ID from the route parameter.  
        /// If the supplier is found, the method sets `IsConfirmByAdmin` to true.  
        /// Changes are persisted to the database using the UnitOfWork.  
        /// Upon success, the system sends a confirmation email to the supplier,  
        /// notifying them that their account has been approved and is now active.  
        /// Returns a success response with a message if approved successfully,  
        /// otherwise returns a BadRequest or NotFound response as appropriate.  
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/suppliers/{id}")]
        public async Task<IActionResult> AcceptSupplier([FromRoute] int id)
        {
            if (id <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صحيح supplier ID."));
            }
            var supplier = await _unitOfWork.Suppliers.FindAsync(s=> s.Id == id, ["User"]);
            if (supplier == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }
            supplier.IsConfirmByAdmin = true;
            if (_unitOfWork.Save() != 0)
            {
                var notificationEmail = new EmailDto()
                {
                    To = supplier.User.Email,
                    Subject = "تمت الموافقة على حسابك - منصة SupplyFi",
                    Body = $@"
                        <div style=""font-family: 'Tajawal', sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                            <h2 style=""color: #28a745; text-align: center;"">تهانينا! حسابك الآن فعال</h2>
                            <p><strong>مرحباً،</strong></p>
                            <p>يسرنا أن نعلمك بأنه قد تمت مراجعة ملفاتك و تمت الموافقة على حسابك بنجاح في منصة SupplyFi.</p>
                            <p>الآن يمكنك تسجيل الدخول إلى لوحة التحكم الخاصة بك والبدء في استخدام جميع خدمات وميزات المنصة.</p>
                            <p style=""text-align: center; margin: 30px 0;"">
                                <a href=""https://b2b-platform-nu.vercel.app/en/login"" style=""background-color: #007bff; color: #fff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">تسجيل الدخول الآن</a>
                            </p>
                            <p>نحن متحمسون للعمل معك ونتطلع إلى رؤية إسهاماتك في منصتنا.</p>
                            <p>بالتوفيق،</p>
                            <p><strong>فريق منصة SuppliFy</strong></p>
                            <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                            <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                        </div>
                    "
                };
                _emailService.SendEmail(notificationEmail);
                return Ok(ResponseFactory.CreateMessageResponse("تم قبول المورد"));
            }
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل قبول المورد"));

        }
        
        /// <summary>
        /// **Function Summary:**  
        /// This method allows an Admin to delete a supplier and their associated user account.  
        /// It first validates the supplier ID from the route parameter.  
        /// If the supplier does not exist, it returns a NotFound response.  
        /// If found, both the Supplier and User records are removed from the database.  
        /// Upon successful deletion, an email is sent to the supplier,  
        /// notifying them that their registration request has been rejected.  
        /// Returns a success response if deletion and email are successful,  
        /// otherwise returns a BadRequest response if saving changes fails.  
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/suppliers/{id}")]
        public async Task<IActionResult> DeleteSupplier([FromRoute] int id)
        {
            if (id <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صحيح supplier ID."));
            }
            var supplier = await _unitOfWork.Suppliers.FindAsync(s=>s.Id == id,new[] {"User"});
            if (supplier == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }
            _unitOfWork.Suppliers.Delete(supplier);
            _unitOfWork.Users.Delete(supplier.User);

            if (_unitOfWork.Save() != 0)
            {
                var notificationEmail = new EmailDto()
                {
                    To = supplier.User.Email,
                    Subject = "بخصوص طلب التسجيل الخاص بك - منصة SupplyFi",
                    Body = $@"
                            <div style=""font-family: 'Tajawal', sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
                                <h2 style=""color: #dc3545; text-align: center;"">نأسف، لم يتم قبول طلبك</h2>
                                <p><strong>مرحباً،</strong></p>
                                <p>بعد مراجعة الملفات التي قدمتها، نأسف لإبلاغك بأنه لم يتم الموافقة على طلب تسجيلك في منصة SupplyFi في الوقت الحالي.</p>
                                <p>قد يكون السبب هو عدم اكتمال بعض المستندات أو وجود معلومات غير صحيحة. يرجى مراجعة ملفاتك والتأكد من أنها تفي بجميع المتطلبات.</p>
                                <p>إذا كانت لديك أي أسئلة أو كنت بحاجة إلى مساعدة، يمكنك التواصل معنا مباشرة من خلال البريد الإلكتروني.</p>
                                <p>بالتوفيق،</p>
                                <p><strong>فريق منصة SuppliFy</strong></p>
                                <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
                                <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
                            </div>
                            "
                };
                _emailService.SendEmail(notificationEmail);
                return Ok(ResponseFactory.CreateMessageResponse("تم حذف المورد بنجاح"));
            }
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل حذف المورد حاول مرة أخرى"));
        }
        /// <summary>
        /// **Function Summary:**  
        /// Retrieves detailed information about a supplier based on the provided user ID.  
        /// Validates the ID before processing; returns BadRequest if invalid.  
        /// If the user or supplier does not exist, a NotFound response is returned.  
        /// If found, it fetches supplier details including profile, categories,  
        /// products, locations, accepted orders, and reviews with average rating.  
        /// Returns a formatted response containing the supplier data.  
        /// </summary>
        /// <param name="id">The unique identifier of the supplier's user account.</param>
        /// <returns>
        /// 200 OK with supplier information if successful.  
        /// 400 BadRequest if the supplier ID is invalid.  
        /// 404 NotFound if the supplier or user does not exist.  
        /// </returns>
        [HttpGet("supplier-info/{id}")]
        public async Task<IActionResult> GetSupplierInfo([FromRoute] int id)
        {
            if (id <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صحيح supplier ID."));
            }
            var user = await _unitOfWork.Users.FindAsync(x => x.Id == id, new[] { "Supplier", "Supplier.SupplierCategories.Category", "Supplier.Products" });
                
            if (user == null || user.Supplier == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }
            var reviews = await _unitOfWork.Reviews.FindAllAsync(r => r.RevieweeId == user.Supplier.Id);
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var supplierDto = new SupplierDataDto
            {
                Id= user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.Phone,
                LogoURL = user.Supplier?.LogoURL ?? string.Empty,
                Description = user.Supplier?.Description ?? string.Empty,
                Locations = user.Supplier?.Locations ?? new List<string>(),
                CountOfOrderAccepted = user.Supplier?.CountOfOrderAccepted ?? 0,
                AverageRating = averageRating,
                ProductCount = user.Supplier?.Products?.Count ?? 0,
                Categories = user.Supplier?.SupplierCategories
                    .Where(sc => sc.Category != null)
                    .Select(sc => sc.Category.Name)
                    .ToList() ?? new List<string>()
            };
            return Ok(ResponseFactory.CreateGeneralSingleResponse(supplierDto));
        }

        /// <summary>
        /// **Function Summary:**  
        /// Updates supplier profile information (description, locations, and categories)  
        /// based on the provided supplier ID and request body data.  
        /// Validates input and ensures the supplier exists before applying updates.  
        /// If categories are updated, old ones are removed and replaced with new entries.  
        /// Returns success or failure message depending on the update result.  
        /// </summary>
        /// <param name="id">The unique identifier of the supplier's user account.</param>
        /// <param name="supplierData">The DTO containing editable supplier information (description, locations, categories).</param>
        /// <returns>
        /// 200 OK if the supplier data is updated successfully.  
        /// 400 BadRequest if input data is invalid or update fails.  
        /// 404 NotFound if the supplier does not exist.  
        /// </returns>
        [Authorize(Roles = "Suppliers")]
        [HttpPatch("supplier/supplier-info/{id}")]
        public async Task<IActionResult> UpdateSupplierInfo([FromRoute] int id, [FromBody] SupplierEditDto supplierData)
        {
            if (id <= 0 || supplierData == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("بيانات غير صحيحة"));
            }

            var user = await _unitOfWork.Users.FindAsync(x => x.Id == id, new[] { "Supplier.SupplierCategories.Category" });
            if (user == null || user.Supplier == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }

            // Update description if provided
            if (!string.IsNullOrWhiteSpace(supplierData.Description))
            {
                user.Supplier.Description = supplierData.Description;
            }

            // Update locations if provided
            if (supplierData.Locations != null && supplierData.Locations.Count > 0)
            {
                user.Supplier.Locations = supplierData.Locations;
            }
            int res = 0;
            if (supplierData.CategoriesId != null && supplierData.CategoriesId.Count > 0)
            {
                _unitOfWork.SupplierCategories.DeleteRange(user.Supplier.SupplierCategories);
                List<SupplierCategory> supplierCategories = new List<SupplierCategory>();
                foreach (var catId in supplierData.CategoriesId)
                {
                    if (_unitOfWork.Categories.GetById(catId) == null)
                        continue;
                    supplierCategories.Add(new SupplierCategory() 
                    {
                        CategoryId = catId,
                        SupplierId = user.Supplier.Id,
                    });
                }
                await _unitOfWork.SupplierCategories.AddRangeAsync(supplierCategories);
                res = await _unitOfWork.SaveAsync();
            }
            if (res != 0)
            {
                return Ok(ResponseFactory.CreateMessageResponse("تم تحديث بيانات المورد بنجاح"));
            }

            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في تحديث بيانات المورد"));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method allows a Supplier to update their logo.  
        /// It validates the supplier ID, file type (JPEG/PNG), and file size (max 5 MB).  
        /// If a previous logo exists, it is deleted from Cloudinary before uploading the new one.  
        /// Upon successful upload, the Supplier’s logo URL and PublicId are updated in the database.  
        /// Returns success response if updated, otherwise returns an appropriate error response.  
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPatch("supplier/supplier-logo/{id}")]
        public async Task<IActionResult> UpdateSupplierLogo([FromRoute] int id, [FromForm] ImageUploadDto logoFile)
        {
            if (id <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صحيح ID"));
            var user = await _unitOfWork.Users.FindAsync(x => x.Id == id, new[] { "Supplier" });
            if (user == null || user.Supplier == null)
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            if(logoFile.Logo == null || logoFile.Logo.Length == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("اللوجو مطلوب"));
            var file = logoFile.Logo;
            var allowedTypes = new[] { "image/jpeg", "image/jpg","image/png" };
            var allowedExtensions = new[] { ".jpg", ".jpeg",".png" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedTypes.Contains(file.ContentType.ToLower()) || !allowedExtensions.Contains(extension))
                return BadRequest(ResponseFactory.CreateMessageResponse("فقط .jpg or .jpeg الصور من نوع"));
            // Upload the new logo to ImageBB
            if (logoFile.Logo.Length < 5 * 1024 * 1024) // Check if the file size is less than 5 MB
            {
                if (!string.IsNullOrEmpty(user.Supplier.ImagePublicId))
                    await _cloudinaryService.DeleteAsync(user.Supplier.ImagePublicId);
                CloudinaryUploadResultDto result = await _cloudinaryService.UploadImageAsync(logoFile.Logo);
                if (string.IsNullOrEmpty(result.Url))
                    return BadRequest(ResponseFactory.CreateMessageResponse("فشل تحميل صورة المنتج"));
                user.Supplier.ImagePublicId = result.PublicId;
                user.Supplier.LogoURL = result.Url;
            }
            else
                return BadRequest(ResponseFactory.CreateMessageResponse(" 5 MB أقصى حجم"));
            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم تحديث اللوجو بنجاح"));
            return BadRequest(ResponseFactory.CreateMessageResponse(" فشل تحديث اللوجو حاول مرة أخرى"));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves the profile information of the currently logged-in Supplier.  
        /// It extracts the Supplier's ID from the Claims, fetches the Supplier with related data  
        /// (categories, subscription plan, and reviews), and calculates the average rating.  
        /// Returns a detailed `SupplierProfileDto` including personal info, subscription,  
        /// categories, product count, and rating.  
        /// Returns 404 if the supplier is not found, otherwise returns the profile data.  
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpGet("supplier/profile")]
        public async Task<IActionResult> GetSupplierProfile()
        {
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("there is problems in Claims"));
            var user = await _unitOfWork.Users.FindAsync(x => x.Id == userId, new[] { "Supplier.SupplierCategories.Category", "Supplier.SupplierSubscriptionPlan.SubscriptionPlan" });
            var reviews = await _unitOfWork.Reviews.FindAllAsync(r => r.RevieweeId == user.Supplier.Id);
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            if (user == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }
            var supplierDto = new SupplierProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.Phone,
                LogoURL = user.Supplier?.LogoURL ?? string.Empty,
                Description = user.Supplier?.Description ?? string.Empty,
                Locations = user.Supplier?.Locations ?? new List<string>(),
                PlanName = user.Supplier?.SupplierSubscriptionPlan?.SubscriptionPlan?.Name ?? "Not Subscribed",
                AverageRating = averageRating,
                ProductCount = user.Supplier?.Products?.Count ?? 0,
                SubscriptionStartDate = user.Supplier?.SupplierSubscriptionPlan?.StartDate ?? DateTime.MinValue,
                SubscriptionEndDate = user.Supplier?.SupplierSubscriptionPlan?.EndDate ?? DateTime.MinValue,
                Categories = user.Supplier?.SupplierCategories
                    .Where(sc => sc.Category != null)
                    .Select(sc => sc.Category.Id)
                    .ToList() ?? new List<int>()
            };
            return Ok(ResponseFactory.CreateGeneralSingleResponse(supplierDto));
        }
        /// <summary>
        /// Tracks and retrieves analytics for supplier profile views.
        /// <para>
        /// Accepts a <c>userId</c> from the route, validates it, and checks if the corresponding supplier exists.  
        /// If the supplier is found and the request has not been recently cached (based on client IP and User-Agent),
        /// their <c>NumberOfViews</c> is incremented and persisted to the database.
        /// </para>
        /// <para>
        /// The view count is only updated once within a 15-minute cache window to avoid counting repeated
        /// refreshes or rapid revisits by the same user.
        /// </para>
        /// <para>
        /// Returns:
        /// <list type="bullet">
        ///   <item><description><c>Ok</c>: With the updated view count.</description></item>
        ///   <item><description><c>BadRequest</c>: If the <c>userId</c> is invalid or saving fails.</description></item>
        ///   <item><description><c>NotFound</c>: If the supplier does not exist.</description></item>
        /// </list>
        /// </para>
        /// </summary>
        [HttpGet("analytics/suppliers/views/{userId:int}")]
        public async Task<IActionResult> GetSupplierViewsAnalytics(
        [FromRoute] int userId,
        [FromServices] IMemoryCache cache)
        {
            if (userId <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("رقم المستخدم غير صحيح"));

            var supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId);
            if (supplier == null)
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));

 
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown_agent";
            var cacheKey = $"SupplierView_{userId}_{clientIp}_{userAgent}";

 
            if (!cache.TryGetValue(cacheKey, out _))
            {
                supplier.NumberOfViews += 1;
                _unitOfWork.Suppliers.Update(supplier);

                if (await _unitOfWork.SaveAsync() != 0)
                {
                    cache.Set(cacheKey, true, TimeSpan.FromMinutes(15));
                    return Ok(ResponseFactory.CreateMessageResponse($"{supplier.NumberOfViews}"));
                }

                return BadRequest(ResponseFactory.CreateMessageResponse("فشل في تحديث عدد مشاهدات المورد"));
            }

        
        return Ok(ResponseFactory.CreateMessageResponse($"{supplier.NumberOfViews}"));
        }

    }
}
