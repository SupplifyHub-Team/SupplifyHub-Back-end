using API.DTO;
using API.DTO.Categories;
using API.DTO.GeneralResponse;
using API.Factory;
using API.ImageResponse;
using CloudinaryDotNet.Actions;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.Entities;
using Org.BouncyCastle.Bcpg;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using YourNamespace;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace API.Controllers
{

    [Route("api/")]
    
    [ApiController]
    public class CategoriesController : APIBaseController
    {
        private readonly ICloudinaryService _cloudinaryService;
        public CategoriesController(IUnitOfWork unitOfWork, IConfiguration config, ICloudinaryService cloudinary) : base(unitOfWork,config) 
        {
            _cloudinaryService = cloudinary;
        }

        /// <summary>
        /// **Function Summary:**
        /// This method, restricted to users with the "Admin" role,
        /// handles the creation of a new category.
        ///
        /// It receives a category name and an image file.
        /// The process begins by validating the image file's size and type (JPG/PNG).
        /// If the image is valid, it's uploaded to a cloud service.
        ///
        /// Before saving the new category, it checks for duplicate names in the database.
        /// If no duplicates are found, a new `Category` object is created with the provided data and uploaded image details, and then saved to the database.
        ///
        /// A success message is returned upon a successful creation, or an appropriate error response is given for any validation failures, upload issues, or conflicts.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/category")]
        public async Task<IActionResult> AdminAddCategory([FromForm] AddCategoryDto formDto)
        {
            try
            {
                ValidatePhoto(formDto.Photo);
                if(!ModelState.IsValid)
                    return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
                CloudinaryUploadResultDto result;
                try
                {
                    result = await _cloudinaryService.UploadImageAsync(formDto.Photo);
                    if (string.IsNullOrEmpty(result.Url))
                        return BadRequest(ResponseFactory.CreateMessageResponse("فشل في تحميل الصورة"));
                }
                catch (Exception ex)
                {
                    // Log error here (ex)
                    return StatusCode(500, ResponseFactory.CreateMessageResponse("Image upload failed: " + ex.Message));
                }

                // 4. Check for duplicate category
                var existingCategory = await _unitOfWork.Categories.GetByColumnAsync("Name", formDto.CategoryName);
                if (existingCategory != null)
                    return Conflict(ResponseFactory.CreateMessageResponse("الفئة موجودة بالفعل"));

                // 5. Create new category
                var newCategory = new Category
                {
                    Name = formDto.CategoryName,
                    PhotoURL =  result.Url,
                    ImagePublicId = result.PublicId,
                    CategoryType = CategoryType.Order,
                    CategoryStatus = CategoryStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                // 6. Save to database
                _unitOfWork.Categories.Add(newCategory);
                return _unitOfWork.Save() != 0
                    ? Ok(ResponseFactory.CreateMessageResponse("تم إضافة الفئة بنجاح"))
                    : BadRequest(ResponseFactory.CreateMessageResponse("فشل في حذف الفئة"));
            }
            catch (Exception ex)
            {
                // Log exception here (ex)
                return StatusCode(500, ResponseFactory.CreateMessageResponse("Internal server error"));
            }
        }

        /// <summary>
        /// **Function Summary:**
        /// This method retrieves a paginated and sortable list of active categories.
        ///
        /// It supports filtering categories by name using a search query.
        /// For each category, it calculates two key metrics: the number of associated suppliers and clients.
        ///
        /// The supplier count is determined by counting supplier-category relationships for users with the "Suppliers" role.
        /// The client count is calculated by counting unique user IDs from all orders associated with that category.
        ///
        /// The method returns a paginated response containing the categories, along with the calculated counts and pagination metadata.
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories(
            string? search,
            int page = 1,
            int pageSize = 10,
            string sortColumn = "CreatedAt",
            string sortColumnDirection = "Desc")
        {
            Expression<Func<Category, bool>> criteria = c =>
                c.CategoryStatus == CategoryStatus.Active &&
                (string.IsNullOrEmpty(search) || c.Name.ToLower().Contains(search.ToLower()));

            int totalCount = await _unitOfWork.Categories.CountAsync(criteria);
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var clientCountsPerCategory = orders
                .GroupBy(o => o.CategoryId)
                .ToDictionary(
                g => g.Key,
                g => g.Select(o => o.UserId).Distinct().Count()
            );
            var categories = await _unitOfWork.Categories.FindWithFiltersAsync(
                4,
                 criteria: criteria,
                sortColumn: sortColumn,
                sortColumnDirection: sortColumnDirection,
                skip: (page - 1) * pageSize,
                take: pageSize
                );

            
            var result = categories.Select(c => new AdminCategoryDto
            {
                CategoryId = c.Id,
                ImageURL = c.PhotoURL,
                CategoryName = c.Name,
                // Count companies with a user having the "supplier" role
                NumberOfAssociatedSuppliers = c.SupplierCategorys?
                    .Count(cc => cc.Supplier != null
                        && cc.Supplier.User != null
                        && cc.Supplier.User.UserRoles.Any(ur => ur.Role.Name == RoleName.Suppliers)) ?? 0,
                // Count individuals with a user having the "client" role  ------>     /// logic will change
                NumberOfAssociatedClients = clientCountsPerCategory
                    .TryGetValue(c.Id, out var count) ? count : 0
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
        /// This method, restricted to users with the "Admin" role,
        /// retrieves a list of categories that have been requested by users and are awaiting approval.
        ///
        /// It fetches all `UserRequestCategories` that are associated with a user.
        /// The result is a list of objects containing the category ID, name, and the requesting user's name and email.
        /// If a user's details are not available, "Unknown" is used as a fallback.
        ///
        /// The method returns a successful response with the formatted list of pending categories.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/category-to-accept")]
        public async Task<IActionResult> GetCategoryToBeAccepted()
        {
            var categoriesToAccept = await _unitOfWork.UserRequestCategories.FindAllAsync(s => s.UserId != 0, new[] {"User"});
            var result = categoriesToAccept.Select(c => new
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                UserName = c.User != null ? c.User.Name : "Unknown",
                UserEmail = c.User != null ? c.User.Email : "Unknown"
            }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(result));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method, restricted to users with the "Admin" role,
        /// handles the acceptance of a user-requested category.
        ///
        /// It receives the ID of a pending category and checks if it exists in the database.
        /// If found, it creates a new official category, using the name from the request.
        /// A default image URL and public ID are assigned to the new category.
        /// The method then deletes the original request from the pending list and saves both changes to the database.
        ///
        /// A success message is returned if the category is activated, or a failure message if the activation fails or the category is not found.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/category/{categoryId}")]
        public async Task<IActionResult> AdminAcceptCategory([FromRoute] int categoryId)
        {
            var categoryToAdd = await _unitOfWork.UserRequestCategories.FindAsync(c => c.Id == categoryId);
            if (categoryToAdd == null)
            {
                return Ok(ResponseFactory.CreateMessageResponse("الفئة المقترحة غير موجودة"));
            }
            // Assuming you have a method to accept the category
            var category= new Category
            {
                Name = categoryToAdd.Name,
                PhotoURL = "https://res.cloudinary.com/dl2v9azqw/image/upload/v1757259229/112233_ciyjss.jpg", // Set a default image or handle as needed
                ImagePublicId = "", // Set a default public ID or handle as needed
                CategoryType = CategoryType.Order,
                CategoryStatus = CategoryStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Categories.AddAsync(category);
            _unitOfWork.UserRequestCategories.Delete(categoryToAdd);
            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم تفعيل الفئة بنجاح"));
            return Ok(ResponseFactory.CreateMessageResponse("فشل في تفعيل الفئة"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method, restricted to users with the "Admin" role,
        /// handles the deletion of a user-requested category that is awaiting approval.
        ///
        /// It receives the ID of the pending category to be deleted.
        /// The method first validates that the provided ID is a positive number.
        /// It then checks if the category exists in the `UserRequestCategories` table.
        /// If the category is found, it is deleted from the database.
        ///
        /// A success message is returned if the deletion is successful, or an error message if the category is not found or the deletion fails.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/category/{categoryId}")]
        public async Task<IActionResult> AdminDeleteCategory(int categoryId)
        {
            if(categoryId <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("الرقم الخاص بالفئة يجب أن يكون رقم موجب"));
            var categoryToDelete = await _unitOfWork.UserRequestCategories.FindAsync(c => c.Id == categoryId);
            if(categoryToDelete is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("الفئة المختارة غير موجودة"));
            _unitOfWork.UserRequestCategories.Delete(categoryToDelete);

            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم حذف الفئة بنجاح"));

            return BadRequest(ResponseFactory.CreateMessageResponse("فشل حذف الفئة"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to update an existing category.
        ///
        /// It receives the updated category information, including a new photo, from a form.
        /// The method first validates the new photo and other data.
        /// It then retrieves the existing category from the database using its ID and updates the name.
        /// The existing image associated with the category is deleted from Cloudinary, and the new image is uploaded.
        /// The category's photo URL and public ID are updated, and all changes are saved to the database.
        ///
        /// The method returns a success message if the update is successful or an error message if any step,
        /// such as photo validation, image upload/deletion, or database saving, fails.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/category")]
        public async Task<IActionResult> AdminUpdateCategory([FromForm] UpdateCategoryDto updateCategoryDto)
        {
            ValidatePhoto(updateCategoryDto.Photo);
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            Category category = await _unitOfWork.Categories.GetByIdAsync(updateCategoryDto.CategoryId);
            category.Name = updateCategoryDto.CategoryName;
            CloudinaryUploadResultDto imageResult;
            try
            {
                var result = true;

                if (!string.IsNullOrEmpty(category.ImagePublicId))
                    result = await _cloudinaryService.DeleteAsync(category.ImagePublicId);

                if (!result)
                    return BadRequest(ResponseFactory.CreateMessageResponse("مشكلة في حذف الصورة القديمة"));

                // Proceed to upload new image
                imageResult = await _cloudinaryService.UploadImageAsync(updateCategoryDto.Photo);

            }
            catch (Exception)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("مشكلة في رفع الصورة"));
            }
            category.PhotoURL = imageResult.Url;
            category.ImagePublicId = imageResult.PublicId;
            _unitOfWork.Categories.Update(category);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل تحديث الفئة"));
            return Ok(ResponseFactory.CreateMessageResponse("تم تحديث الفئة بنجاح"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows any authenticated user to suggest a new category.
        ///
        /// It receives a category name from the request body and validates the input.
        /// It then extracts the user's ID from their authentication claims to associate the suggestion with a user.
        /// Before creating a new entry, the method checks if an active category with the same name already exists in the database to prevent duplicates.
        /// If the category does not exist, it creates a new entry in the `UserRequestCategories` table for administrative review and saves the change.
        ///
        /// A success message is returned if the suggestion is submitted successfully, otherwise, an error message is returned.
        /// </summary>
        [Authorize]
        [HttpPost("categories/suggest")]
        public async Task<IActionResult> SuggestCategories([FromBody] SuggestCategoryDto suggestCategoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
            if (userId == null || userId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح user ID."));
            }
            var existingCategory = await _unitOfWork.Categories.FindAsync(c => c.Name.ToLower() == suggestCategoryDto.Name.ToLower() && c.CategoryStatus == CategoryStatus.Active);
            if (existingCategory != null)
                return BadRequest(ResponseFactory.CreateMessageResponse("الفئة موجودة بالفعل"));
            // default image must be added here
            var newCategory = new UserRequestCategory
            {
                Name = suggestCategoryDto.Name,
                UserId = userId.Value,
            };
            await _unitOfWork.UserRequestCategories.AddAsync(newCategory);
            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("اقتراح الفئة تم بنجاح"));
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل اقتراح الفئة حاول مجددا"));
        }
        

    }
}
