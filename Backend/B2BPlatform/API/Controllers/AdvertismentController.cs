using API.DTO;
using API.DTO.Advertisment;
using API.DTO.Categories;
using API.DTO.GeneralResponse;
using API.Factory;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.Entities;
using System.Linq.Expressions;
using System.Security.Claims;
using YourNamespace;
/// <summary>
/// **Controller Summary:**
/// This controller manages all advertisement-related operations for both Suppliers and Admins.
/// It handles creating, listing, approving, and deleting advertisements, as well as processing
/// requests for additional advertisement slots.
/// </summary>
namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class AdvertismentController : APIBaseController
    {
        private readonly ICloudinaryService _cloudinaryService;

        public AdvertismentController(IUnitOfWork unitOfWork,IConfiguration configuration,ICloudinaryService cloudinary) : base(unitOfWork,configuration) 
        {
              _cloudinaryService = cloudinary;
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an authenticated supplier to upload a new advertisement.
        ///
        /// It receives advertisement details and an image file.
        /// The method validates the image and ensures the user is a registered supplier with an active subscription.
        /// It verifies if the supplier has available advertisement slots based on their subscription plan.
        /// If all conditions are met, the image is uploaded to a cloud service.
        /// A new advertisement record is created in the database with the image URL and is initially set to inactive, pending administrative approval.
        /// The number of remaining advertisements in the supplier's subscription plan is then decremented.
        /// The method returns a success message if the advertisement is saved, or an error message if any validation, upload, or database operation fails.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/advertisement")]
        public async Task<IActionResult> UploadAdvertisementImage([FromForm] AdvertismentPostDto AdDto)
        {
            ValidatePhoto(AdDto.ImageFile);
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            
           

            var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
            if (userId == null || userId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح user ID."));
            }

            var user = await _unitOfWork.Users.FindAsync(x => x.Id == userId, new[] { "Supplier.SupplierAdvertisements" });
            if (user == null || user.Supplier == null)
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans
                    .FindAsync(sp => sp.SupplierId == user.Supplier.Id, new[] { "Supplier.User", "SubscriptionPlan" });
            if (existingSubscription == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("اشترك في خطة أعلى أولا"));
            }
            if (existingSubscription.NumberOfAdvertisement <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("لقد وصلت لأقصى عدد من الاعلانات "));
            }

            var uploadResult = await _cloudinaryService.UploadImageAsync(AdDto.ImageFile);
            if (string.IsNullOrEmpty(uploadResult.Url))
                return BadRequest(ResponseFactory.CreateMessageResponse("مشكلة في تحميل الصورة"));
            // Create a new advertisement
            var advertisement = new SupplierAdvertisement
            {
                Title = AdDto.Title,
                ImageUrl = uploadResult.Url,
                ImagePublicId= uploadResult.PublicId,
                TargetUrl = AdDto.TargetUrl ?? string.Empty,
                StartDate = DateTime.UtcNow,
                EndDate = existingSubscription.EndDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = false,
                SupplierId = user.Supplier.Id
            };
            await _unitOfWork.SupplierAdvertisements.AddAsync(advertisement);
            existingSubscription.NumberOfAdvertisement -= 1;
            _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
            if (await _unitOfWork.SaveAsync() != 0)
            {
                return Ok(ResponseFactory.CreateMessageResponse("تم رفع الاعلان للأدمن بنجاح"));
            }
                
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل حفظ الاعلان "));
            
        }
        
        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to directly add a new advertisement.
        ///
        /// It receives advertisement details and an image file from a form.
        /// The method validates the image, and if it's valid, uploads it to a cloud service.
        /// A new advertisement record is created in the database with the provided details, including the image URL.
        /// Unlike supplier-uploaded ads, this ad is set to be immediately active (`IsActive = true`) and is not associated with a specific supplier (`SupplierId = null`).
        /// The new advertisement record is then saved to the database.
        ///
        /// The method returns a success message if the advertisement is added successfully or an error message if any step,
        /// such as photo validation, image upload, or database saving, fails.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/advertisment")]
        public async Task<IActionResult> AddAdvertisementAsync([FromForm] AdminPostAdvertisementDto AdDto)
        {
            ValidatePhoto(AdDto.ImageFile);
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            var uploadResult = await _cloudinaryService.UploadImageAsync(AdDto.ImageFile);
            if (string.IsNullOrEmpty(uploadResult.Url))
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل تحميل الصورة"));
            var advertisement = new SupplierAdvertisement
            {
                Title = AdDto.Title,
                ImageUrl = uploadResult.Url,
                TargetUrl = AdDto.TargetUrl ?? string.Empty,
                StartDate = DateTime.UtcNow,
                EndDate = AdDto.EndDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ImagePublicId = uploadResult.PublicId,
                IsActive = true,
                SupplierId = null // Admin added ads are not linked to any supplier
            };
            await _unitOfWork.SupplierAdvertisements.AddAsync(advertisement);
            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم إضافة الإعلان بنجاح"));
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل إضافة الإعلان"));
        }


        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to retrieve a paginated list of all active advertisements.
        ///
        /// It supports filtering by advertisement title or associated supplier name, and it allows for custom sorting and pagination.
        /// The method first defines a search criteria to find only active advertisements that match the search query.
        /// It then retrieves the total count of matching advertisements for pagination metadata and fetches the specific page of results.
        /// The results are then sorted to ensure that supplier-uploaded ads are displayed before those added by an admin.
        /// Finally, the method maps the data to a DTO and returns a paginated response that includes the list of advertisements and metadata.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/advertisment")]
        public async Task<IActionResult> GetAllAdvertisment(
            string? search,
            int page = 1,
            int pageSize = 10,
            string sortColumn = "StartDate",
            string sortColumnDirection = "Desc")
        {
            // Create criteria with search functionality
            Expression<Func<SupplierAdvertisement, bool>> criteria = ad =>
                ad.IsActive == true &&
                (string.IsNullOrEmpty(search) || ad.Title.Contains(search) || (ad.Supplier != null && ad.Supplier.User.Name.Contains(search)));

            // Get total count for pagination
            int totalCount = await _unitOfWork.SupplierAdvertisements.CountAsync(criteria);

            // Get paginated results with sorting
            var advertisements = await _unitOfWork.SupplierAdvertisements.FindWithFiltersAsync(
                5, // New queryId for advertisements
                criteria: criteria,
                sortColumn: sortColumn,
                sortColumnDirection: sortColumnDirection,
                skip: (page - 1) * pageSize,
                take: pageSize
            );
            advertisements = advertisements
            .OrderBy(ad => ad.SupplierId != null)
            .ToList();

            var result = advertisements.Select(adv => new AdvertismentToBeShownDto
            {
                Id = adv.Id,
                Title = adv.Title,
                TargetUrl = adv.TargetUrl,
                ImageUrl = adv.ImageUrl,
                CompanyName = adv.Supplier is null ? "Admin" : adv.Supplier.User.Name,
                StartDate = adv.StartDate,
                EndDate = adv.EndDate,
                CreatedAt = adv.CreatedAt,
                UpdatedAt = adv.UpdatedAt
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
        /// This method retrieves a randomized list of active and current advertisements to display to a client.
        ///
        /// It fetches all advertisements that are active and within their valid date range.
        /// The ads are then separated into two lists: those uploaded by an administrator and those uploaded by suppliers.
        /// If the request includes a valid `userId` that corresponds to a supplier, the method returns only that supplier's advertisements.
        /// Otherwise, it returns a combined list of all admin-uploaded ads and all supplier-uploaded ads.
        /// The order of the advertisements in the final list is randomized to provide a fresh view on each call.
        /// The method maps the final list to a simplified DTO for the client and returns a general response.
        /// </summary>
        [HttpGet("advertisment-client")]
        public async Task<IActionResult> GetAllAdvertismentForClient([FromQuery] int? userId)
        {
            // نجيب كل الإعلانات النشطة
            var allAds = await _unitOfWork.SupplierAdvertisements.FindAllAsync(ad =>
                ad.IsActive &&
                ad.StartDate <= DateTime.UtcNow &&
                ad.EndDate >= DateTime.UtcNow
            );

            // فلترة الأدمن
            var adminAds = allAds
                .Where(ad => ad.SupplierId == null)
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            List<SupplierAdvertisement> finalAds;

            if (userId.HasValue)
            {
                var supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId.Value);

                if (supplier != null)
                {

                    finalAds = allAds
                        .Where(ad => ad.SupplierId == supplier.Id)
                        .OrderBy(_ => Guid.NewGuid())
                        .ToList();
                }
                else
                {

                    finalAds = adminAds.Concat(
                        allAds.Where(ad => ad.SupplierId != null).OrderBy(_ => Guid.NewGuid())
                    ).ToList();
                }
            }
            else
            {

                finalAds = adminAds.Concat(
                    allAds.Where(ad => ad.SupplierId != null).OrderBy(_ => Guid.NewGuid())
                ).ToList();
            }


            var result = finalAds.Select(adv => new AdvertismentClintShownDto
            {
                Id = adv.Id,
                TargetUrl = adv.TargetUrl,
                ImageUrl = adv.ImageUrl,
            }).ToList();

            return Ok(ResponseFactory.CreateGeneralResponse(result));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to retrieve a list of all advertisements awaiting approval.
        ///
        /// It queries the database to find all `SupplierAdvertisement` entries where the `IsActive` status is `false`.
        /// The method also includes the associated supplier's user information to display the company name.
        /// The retrieved data is then mapped to a simplified DTO (`AdvertismentToBeShownDto`) containing essential details such as the ad's ID, title, image URL, and the name of the supplier who submitted it.
        /// The primary purpose of this endpoint is to provide a list of pending advertisements for an administrator to review and take action on.
        /// The method returns a general success response with the list of advertisements to be accepted.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/advertisment-to-accept")]
        public async Task<IActionResult> GetAllAdvertismentToAccept()
        {
            var Advertisment = await _unitOfWork.SupplierAdvertisements.FindAllAsync(ad => ad.IsActive==false, new[] {"Supplier.User"});
            var result =  Advertisment.Select(adv=> new AdvertismentToBeShownDto
            {
                Id = adv.Id,
                Title = adv.Title,
                TargetUrl = adv.TargetUrl,
                ImageUrl = adv.ImageUrl,
                CompanyName = adv.Supplier.User.Name,
                StartDate = adv.StartDate,
                EndDate = adv.EndDate,
                CreatedAt = adv.CreatedAt,
                UpdatedAt = adv.UpdatedAt
            }).ToList(); ;
            return Ok(ResponseFactory.CreateGeneralResponse(result));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to approve a submitted advertisement.
        ///
        /// It receives an advertisement ID from the URL and validates that it is a positive integer.
        /// The method then attempts to find the corresponding advertisement in the database. If the ad is found, its `IsActive` status is set to `true`, and the `UpdatedAt` timestamp is updated.
        /// Finally, it saves the changes to the database. If the save operation is successful, it returns a success message; otherwise, it returns an error message indicating that the approval failed.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/advertisment/{advirtismentId}")]
        public async Task<IActionResult> AcceptAdvertisement([FromRoute] int advirtismentId)
        {
            if (advirtismentId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح advertisement ID."));
            }
            var advertisement = await _unitOfWork.SupplierAdvertisements.FindAsync(ad => ad.Id == advirtismentId);
            if (advertisement == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("إعلان غير موجود"));
            }
            advertisement.IsActive = true;
            advertisement.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.SupplierAdvertisements.Update(advertisement);
            if (_unitOfWork.Save() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم قبول الإعلان"));
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في قبول الإعلان"));
            
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to delete a specific advertisement.
        ///
        /// It receives the advertisement ID from the URL. After validating the ID, it searches for the corresponding advertisement in the database, including the associated supplier information.
        /// If the advertisement is found, it first deletes the related image from the cloud storage. It then increments the number of available ad slots on the supplier's subscription plan, as the deleted ad frees up a space.
        /// Finally, the method deletes the advertisement record from the database. It returns a success message upon successful deletion, or an error message if the operation fails.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/advertisment/{advirtismentId}")]
        public async Task<IActionResult> DeleteAdvertisement([FromRoute] int advirtismentId)
        {
            if (advirtismentId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح advertisement ID."));
            }
            var advertisement = await _unitOfWork.SupplierAdvertisements.FindAsync(ad => ad.Id == advirtismentId, new[] {"Supplier"});
            if (advertisement == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("إعلان غير موجود"));
            }
            // Delete image from Cloudinary
            await _cloudinaryService.DeleteAsync(advertisement.ImagePublicId);
            var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans
                    .FindAsync(sp => sp.SupplierId == advertisement.Supplier.Id);
            existingSubscription.NumberOfAdvertisement += 1;
            _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
            _unitOfWork.SupplierAdvertisements.Delete(advertisement);
            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم حذف الإعلان بنجاح"));
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في حذف الإعلان"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to delete an active advertisement.
        ///
        /// It validates the provided advertisement ID, retrieves the corresponding advertisement from the database, and checks for its existence.
        /// Upon confirmation, the method deletes the associated image from the cloud storage. It then locates the supplier's subscription plan and increments the count of available advertisement slots.
        /// Finally, the advertisement record is removed from the database. The method returns a success message if the deletion and updates are successful, or an error message if the operation fails.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/active-advertisement/{advirtisementId}")]
        public async Task<IActionResult> DeleteActiveAdvertisement([FromRoute] int advirtisementId)
        {
            if (advirtisementId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح advertisement ID."));
            }

            var advertisement = await _unitOfWork.SupplierAdvertisements
                .FindAsync(ad => ad.Id == advirtisementId, new[] { "Supplier" });

            if (advertisement == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("إعلان غير موجود"));
            }

            // Delete image from Cloudinary
            if (!string.IsNullOrEmpty(advertisement.ImagePublicId))
            {
                await _cloudinaryService.DeleteAsync(advertisement.ImagePublicId);
            }

            // Get subscription plan by SupplierId (مش محتاج Supplier navigation)
            var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans
                .FindAsync(sp => sp.SupplierId == advertisement.SupplierId);

            if (existingSubscription != null)
            {
                existingSubscription.NumberOfAdvertisement += 1;
                _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
            }

            _unitOfWork.SupplierAdvertisements.Delete(advertisement);

            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم حذف الإعلان بنجاح"));

            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في حذف الإعلان"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method retrieves a paginated list of pending requests for new advertisement slots.
        ///
        /// It filters all supplier advertisement requests to find those with a "Pending" status. The results are then paginated and sorted by creation date in descending order.
        /// The method returns a paginated response containing a list of `RequestToAddMoreDto` objects, which includes details such as the request ID, the supplier's name, email, phone number, and the amount of new ad slots they have requested.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/Advertisement-request")]
        public async Task<IActionResult> GetAdvertisementRequests(int page = 1, int pageSize = 10)
        {

            Expression<Func<SupplierAdvertisementRequest, bool>> criteria = r => r.Status == Enum.RequestStatus.Pending;
            int totalItems = await _unitOfWork.SupplierAdvertisementRequests.CountAsync(criteria);
            // 4. Build the query
            var supplierAdvertisementRequests = await _unitOfWork.SupplierAdvertisementRequests.FindWithFiltersAsync(
                8,
                criteria: criteria,
                sortColumn: nameof(SupplierAdvertisementRequest.CreatedAt),
                sortColumnDirection: Enum.OrderBy.Desc.ToString(),
                skip: (page - 1) * pageSize,
                take: pageSize
            );
            var result = supplierAdvertisementRequests.Select(r => new RequestToAddMoreDto
            {
                RequestId = r.Id,
                Name = r.Supplier.User.Name,
                Email = r.Supplier.User.Email,
                Phone = r.Supplier.User.Phone,
                Amount = r.RequestedAmount
            }).ToList();
            return Ok(ResponseFactory.CreatePaginationResponse(result, new Meta
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize), // Fixed
                TotalItems = totalItems
            }));
        }

        /// <summary>
        /// **Function Summary:**
        /// This method allows an administrator to either approve or reject a pending request from a supplier for additional advertisement slots.
        ///
        /// It validates the request ID and the type of operation (Accept or Reject). It then retrieves the pending request along with the associated supplier and their subscription plan.
        /// If the request is accepted, the method updates the request status to 'Approved' and increases the number of available ad slots on the supplier's subscription plan by the requested amount.
        /// If the request is rejected, the method simply updates the status to 'Rejected'. Finally, the changes are saved to the database, and a corresponding success or failure message is returned.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/accept-cancel-advertisement-request")]
        public async Task<IActionResult> AdminAcceptOrCancelAdvertisementRequest(OperationOnRequestType operationType, Guid requestId)
        {
            if (requestId == Guid.Empty)
                return BadRequest(ResponseFactory.CreateMessageResponse("id مطلوب"));
            if (!System.Enum.IsDefined(typeof(OperationOnRequestType), operationType))
                return BadRequest(ResponseFactory.CreateMessageResponse("عملية غير صالحة"));
            SupplierAdvertisementRequest supplierAdvertisementRequest = await _unitOfWork.SupplierAdvertisementRequests.FindAsync(r => r.Id == requestId && r.Status == RequestStatus.Pending,
                ["Supplier", "Supplier.SupplierSubscriptionPlan"]);
            bool check;
            if (supplierAdvertisementRequest is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("طلب غير موجود"));
            supplierAdvertisementRequest.ProcessedAt = DateTime.UtcNow;
            if (operationType == OperationOnRequestType.Accept)
            {
                supplierAdvertisementRequest.Status = RequestStatus.Approved;
                supplierAdvertisementRequest.Supplier.SupplierSubscriptionPlan.NumberOfSpecialProduct += supplierAdvertisementRequest.RequestedAmount;
                check = true;
            }
            else
            {
                supplierAdvertisementRequest.Status = RequestStatus.Rejected;
                check = false;
            }
            _unitOfWork.SupplierAdvertisementRequests.Update(supplierAdvertisementRequest);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse(check ? "فشل قبول الطلب" : "فشل رفض الطلب"));
            return Ok(ResponseFactory.CreateMessageResponse(check ? "تم قبول الطلب" : "تم رفض الطلب"));
        }

    }
}
