using API.DTO.GeneralResponse;
using API.DTO;
using API.DTO.Suppliers;
using API.Factory;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;
using YourNamespace;

namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class QuotaController : APIBaseController
    {
        public QuotaController(IUnitOfWork unitOfWork, IConfiguration configuration, ICloudinaryService cloudinary) : base(unitOfWork, configuration) {}

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to retrieve their remaining product, advertisement, special product, and order acceptance quotas. The data is pulled directly from the supplier's active subscription plan.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is secured to only allow users with the "Suppliers" role to access it.
        /// 2. **User Validation:** It extracts the `userId` from the user's claims and retrieves the corresponding user and their associated supplier and subscription plan in a single database query. It returns an error if the supplier is not found.
        /// 3. **Quota Retrieval and Formatting:** The function then accesses the retrieved `SupplierSubscriptionPlan` to get the remaining counts for products, ads, orders, and special products.
        /// 4. **Display Logic:** For quotas that are effectively unlimited (represented by a value greater than 999), the endpoint uses the unicode infinity symbol (`\u221E`) to provide a more user-friendly representation instead of a large number.
        /// 5. **Response:** A `SupplierQuotaDto` is created with the formatted quota information, and this DTO is returned in a successful `Ok` response.
        /// </summary>
        [Authorize(Roles="Suppliers")]
        [HttpGet("quota/supplier")]
        public async Task<IActionResult> GetSupplierQuota()
        {
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("there is problems in Claims"));
            var user = await _unitOfWork.Users.FindAsync(x => x.Id == userId, new[] { "Supplier.SupplierSubscriptionPlan" });
            if (user == null || user.Supplier == null)
            {
                return NotFound(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }
            var result = new SupplierQuotaDto
            {
                Products = user.Supplier.SupplierSubscriptionPlan.NumberOfProduct <= 999? user.Supplier.SupplierSubscriptionPlan.NumberOfProduct.ToString() : "\u221E",
                Ads = user.Supplier.SupplierSubscriptionPlan.NumberOfAdvertisement <= 999 ? user.Supplier.SupplierSubscriptionPlan.NumberOfAdvertisement.ToString() : "\u221E",
                Orders = user.Supplier.SupplierSubscriptionPlan.NumberOfAcceptOrder <= 999 ? user.Supplier.SupplierSubscriptionPlan.NumberOfAcceptOrder.ToString() : "\u221E",
                SpecialProducts = user.Supplier.SupplierSubscriptionPlan.NumberOfSpecialProduct <= 999 ? user.Supplier.SupplierSubscriptionPlan.NumberOfSpecialProduct.ToString() : "\u221E",
            };
            return Ok(ResponseFactory.CreateGeneralSingleResponse(result));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to submit a request to the platform administrator to add more products to their plan.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Suppliers" role, as enforced by the `[Authorize]` attribute.
        /// 2. **Input Validation:** It first checks if the requested `Amount` is at least 1. If not, it returns a `BadRequest` response with an error message.
        /// 3. **Supplier Identification:** It retrieves the `userId` from the authenticated user's claims to find the corresponding `Supplier` record in the database.
        /// 4. **Request Creation:** A new `SupplierProductRequest` object is instantiated. Its properties, including `CreatedAt`, `RequestedAmount`, and `SupplierId`, are populated from the input DTO and the retrieved supplier data. The `Status` is set to `Pending` by default.
        /// 5. **Database Operation:** The new request is added to the unit of work. The `SaveAsync()` method is then called to persist the changes to the database.
        /// 6. **Response:**
        ///    - If the save operation fails (returns 0), a `BadRequest` is returned.
        ///    - If the save is successful, an `Ok` response is returned, confirming that the request was successfully submitted.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/quota/product")]
        public async Task<IActionResult> RequestToAddMoreProduct(QuotaDto quotaDto)
        {
            if (quotaDto.Amount < 1)
                return BadRequest(ResponseFactory.CreateMessageResponse("يجب أن تكون الكمية واحد أو أكثر"));
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("هناك مشكلة فى Claims"));
            Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId);

            SupplierProductRequest supplierProductRequest = new()
            {
                CreatedAt = DateTime.UtcNow,
                RequestedAmount = quotaDto.Amount,
                Status = Enum.RequestStatus.Pending,
                SupplierId = supplier.Id
            };
            _unitOfWork.SupplierProductRequests.Add(supplierProductRequest);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل عمل الطلب حاول مرة أخرى"));
            return Ok(ResponseFactory.CreateMessageResponse("تم عمل الطلب بنجاح"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to submit a request to the platform administrator to increase their quota for accepting new orders.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Suppliers" role, as enforced by the `[Authorize]` attribute.
        /// 2. **Input Validation:** It validates that the requested `Amount` in the `QuotaDto` is at least 1, returning a `BadRequest` if the condition is not met. It also validates the `userId` from the user's claims.
        /// 3. **Supplier Identification:** It retrieves the `Supplier` record from the database based on the authenticated user's ID.
        /// 4. **Request Creation:** A new `SupplierAcceptOrderRequest` object is created. Its properties, including `CreatedAt`, `RequestedAmount`, and `SupplierId`, are populated. The `Status` is set to `Pending` by default.
        /// 5. **Database Operation:** The new request is added to the unit of work, and the changes are then persisted to the database by calling `SaveAsync()`.
        /// 6. **Response:**
        ///    - If the `SaveAsync()` method returns 0 (indicating failure), a `BadRequest` response is returned.
        ///    - If the save is successful, an `Ok` response confirms that the request was successfully submitted.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/quota/offer")]
        public async Task<IActionResult> RequestToAddMoreAcceptOrder(QuotaDto quotaDto)
        {
            if (quotaDto.Amount < 1)
                return BadRequest(ResponseFactory.CreateMessageResponse("يجب أن تكون الكمية واحد أو أكثر"));
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("هناك مشكلة فى Claims"));
            Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId);

            SupplierAcceptOrderRequest supplierAcceptOrderRequest = new()
            {
                CreatedAt = DateTime.UtcNow,
                RequestedAmount = quotaDto.Amount,
                Status = Enum.RequestStatus.Pending,
                SupplierId = supplier.Id
            };
            _unitOfWork.SupplierAcceptOrderRequests.Add(supplierAcceptOrderRequest);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل عمل الطلب حاول مرة أخرى"));
            return Ok(ResponseFactory.CreateMessageResponse("تم عمل الطلب بنجاح"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to submit a request to the platform administrator to increase their quota for advertisements.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Suppliers" role, as enforced by the `[Authorize]` attribute.
        /// 2. **Input Validation:** It validates that the requested `Amount` in the `QuotaDto` is at least 1, returning a `BadRequest` if the condition is not met. It also validates the `userId` from the user's claims.
        /// 3. **Supplier Identification:** It retrieves the `Supplier` record from the database based on the authenticated user's ID.
        /// 4. **Request Creation:** A new `SupplierAdvertisementRequest` object is created. Its properties, including `CreatedAt`, `RequestedAmount`, and `SupplierId`, are populated. The `Status` is set to `Pending` by default.
        /// 5. **Database Operation:** The new request is added to the unit of work, and the changes are then persisted to the database by calling `SaveAsync()`.
        /// 6. **Response:**
        ///    - If the `SaveAsync()` method returns 0 (indicating failure), a `BadRequest` response is returned.
        ///    - If the save is successful, an `Ok` response confirms that the request was successfully submitted.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/quota/ads")]
        public async Task<IActionResult> RequestToAddMoreAdvertisement(QuotaDto quotaDto)
        {
            if (quotaDto.Amount < 1)
                return BadRequest(ResponseFactory.CreateMessageResponse("يجب أن تكون الكمية واحد أو أكثر"));
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("هناك مشكلة فى Claims"));
            Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId);

            SupplierAdvertisementRequest supplierAdvertisementRequest = new()
            {

                CreatedAt = DateTime.UtcNow,
                RequestedAmount = quotaDto.Amount,
                Status = Enum.RequestStatus.Pending,
                SupplierId = supplier.Id
            };
            _unitOfWork.SupplierAdvertisementRequests.Add(supplierAdvertisementRequest);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل عمل الطلب حاول مرة أخرى"));
            return Ok(ResponseFactory.CreateMessageResponse("تم عمل الطلب بنجاح"));
        }
    
    }
}
