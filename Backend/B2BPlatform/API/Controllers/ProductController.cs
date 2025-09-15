using API.DTO;
using API.DTO.GeneralResponse;
using API.DTO.Products;
using API.Factory;
using CloudinaryDotNet;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Models.Entities;
using System.Linq.Expressions;
using System.Security.Claims;
using YourNamespace;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ProductController : APIBaseController
    {
        private readonly ICloudinaryService _cloudinaryService;
        public ProductController(IUnitOfWork unitOfWork, IConfiguration config, ICloudinaryService cloudinary) : base(unitOfWork,config)
        {
            _cloudinaryService = cloudinary;
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a list of products associated with the authenticated supplier.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is accessible only to users with the "Suppliers" role.
        /// 2. **User ID Retrieval:** It attempts to parse the `userId` from the user's claims. If this fails, it returns a `BadRequest` response.
        /// 3. **Supplier Retrieval:** It finds the supplier record associated with the `userId`, including their `User` details. If the supplier is not found, an error is returned.
        /// 4. **Product Retrieval:** It retrieves all products belonging to the found supplier, including any related `SpecialProduct` details.
        /// 5. **Data Mapping:** The products are mapped to a `ProductToShowDto`, which includes the product's ID, name, description, image URL, price, and information about special offers (if any). It also includes the supplier's company name.
        /// 6. **Response:** A successful `Ok` response is returned, containing the list of `ProductToShowDto` objects.
        /// </summary>
        [Authorize(Roles="Suppliers")]
        [HttpGet("supplier/products")]
        public async Task<IActionResult> GetProductBySupplierId()
        {
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("there is problems in Claims"));
            var supplier = await _unitOfWork.Suppliers.FindAsync(s=>s.UserId == userId , new[] { "User" });
            if(supplier == null)
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صحيح UserId"));
            var products = await _unitOfWork.Products.FindAllAsync(x => x.SupplierId == supplier.Id, new[] {"SpecialProduct"});
            var productsToShow = products.Select(p => new ProductToShowDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                ProductImageURl = p.ProductImageURl,
                Price = p.Price,
                Offer = p.SpecialProduct != null ? p.SpecialProduct.Offer : 0,
                IsSpecial = p.SpecialProduct != null,
                CompanyName = supplier.User.Name 
            }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(productsToShow));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a comprehensive product page for a specific supplier, accessible to any user. It combines the supplier's products, customer reviews, and general statistics into a single response.
        ///
        /// **Flow & Logic:**
        /// 1. **Input Validation:** It validates that the `userId` provided in the route is a positive integer. If not, it returns a `BadRequest` response.
        /// 2. **Supplier Retrieval:** It fetches the supplier's details, including the associated `User` information, using the `userId` from the route. If the supplier is not found, it returns an error.
        /// 3. **Data Fetching:** It performs two primary database queries:
        ///    - It retrieves all **reviews** for the supplier, including the `Reviewer` details.
        ///    - It retrieves all **products** associated with the supplier, including any `SpecialProduct` details.
        /// 4. **Data Aggregation and Mapping:**
        ///    - It maps the fetched reviews to a list of `ReviewDto` objects, handling potential null values for name and comment.
        ///    - It maps the products to a list of `ProductToShowDto` objects, similar to the `GetProductBySupplierId` endpoint, and includes the `CompanyName` from the supplier's user profile.
        ///    - It compiles all the data (products, reviews, and a `StatsDto` containing `NumberOfViews`) into a single `ProductPageDto`.
        /// 5. **Response:** A successful `Ok` response is returned with the aggregated `ProductPageDto`.
        ///
        /// **Key Difference from `GetProductBySupplierId`:**
        /// Unlike the previous endpoint, which was restricted to authenticated suppliers and fetched their own products, this endpoint is public-facing. It uses a `userId` from the route to retrieve information for any supplier, making it suitable for a product-listing page on the frontend.
        /// </summary>
        [HttpGet("productPage/{userId}")]
        public async Task<IActionResult> GetProductPageBySupplierId([FromRoute] int userId)
        {

            if (userId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صحيح id"));
            }

            
            var supplier = await _unitOfWork.Suppliers.FindAsync(
                k => k.UserId == userId,
                new[] { "User" }
            );

            if (supplier == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse(" غير صحيح SupplierId"));
            }

            // Get all reviews for the supplier with Reviewer included
            var reviews = await _unitOfWork.Reviews.FindAllAsync(
                r => r.RevieweeId == supplier.Id,
                includes: new[] { "Reviewer" }  // Make sure to include Reviewer
            );

            // Get supplier's products
            var products = await _unitOfWork.Products.FindAllAsync(
                x => x.SupplierId == supplier.Id, new [] { "SpecialProduct" }
            );

            // Create optimized response
            var response = new ProductPageDto
            {
                
                AllReviews = reviews.Select(r => new ReviewDto
                {
                    ReviewerName = r.Reviewer?.Name ?? "Anonymous",
                    Comment = r.Comment ?? "No comment",
                    Rating = r.Rating
                }).ToList(),
                Products = products.Select(p => new ProductToShowDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    CompanyName = supplier.User.Name,
                    Offer = p.SpecialProduct != null ? p.SpecialProduct.Offer : 0,
                    IsSpecial = p.SpecialProduct != null,
                    Description = p.Description,
                    ProductImageURl = p.ProductImageURl
                }).ToList(),
                Stats = new StatsDto
                {
                    NumberOfViews = supplier.NumberOfViews
                }
            };

            return Ok(ResponseFactory.CreateGeneralSingleResponse(response));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to add a new product. It handles product details, image upload, and verifies that the supplier's subscription plan allows for the new product to be added.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization & User Validation:** The endpoint is restricted to authenticated users with the "Suppliers" role. It first retrieves the `userId` from the user's claims and then uses it to find the corresponding supplier record. If either the user ID is invalid or the supplier is not found, a `BadRequest` response is returned.
        /// 2. **Model State Validation:** It checks the `productDto` for any validation errors and returns a `BadRequest` if the model state is invalid.
        /// 3. **Subscription Plan Check:** It retrieves the supplier's current subscription plan. If no plan exists, or if the plan's limit for the number of products (`NumberOfProduct`) has been reached, a `BadRequest` is returned. A separate check is performed for special products (`IsSpecial`), returning a `BadRequest` if the special product limit (`NumberOfSpecialProduct`) has been reached.
        /// 4. **Image Handling:** The endpoint validates the uploaded product image and then uploads it to a cloud service (Cloudinary). If the upload fails, it returns an error.
        /// 5. **Product Creation & Limits Decrement:**
        ///    - It creates a new `Product` entity with the data from the DTO and the Cloudinary URL.
        ///    - The product is added to the database.
        ///    - The `NumberOfProduct` count on the supplier's subscription plan is decremented.
        ///    - The changes are saved to the database.
        /// 6. **Special Product Handling:** If the product is marked as special (`IsSpecial`), it creates an additional `SpecialProduct` record, decrements the `NumberOfSpecialProduct` count on the subscription plan, and saves the changes again.
        /// 7. **Response:** A success message is returned if all operations are successful. If any save operation fails, a `BadRequest` response is returned with an appropriate error message in Arabic.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/product")]
        public async Task<IActionResult> AddProduct([FromForm] ProductPostDto productDto)
        {
            var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
            if (userId == null || userId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح user ID."));
            }

            var supplier = await _unitOfWork.Suppliers.FindAsync(k => k.UserId == userId, new[] { "User" });
            if (supplier == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("للمورد invalid id "));
            }

            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans.FindAsync(
                x => x.SupplierId == supplier.Id 
            );

            if (existingSubscription == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("يجب ترقية خطتك لإضافة منتجات أو لطلب منتج جديد"));
            }

            if (existingSubscription.NumberOfProduct <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("لقد وصلت إلى الحد الأقصى لعدد المنتجات في خطتك الحالية"));
            }
            if (existingSubscription.NumberOfSpecialProduct <= 0 && productDto.IsSpecial == true)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("لقد وصلت إلى الحد الأقصى لعدد المنتجات المميزة في خطتك الحالية"));
            }
            
            ValidatePhoto(productDto.ProductImage);

            CloudinaryUploadResultDto result = await _cloudinaryService.UploadImageAsync(productDto.ProductImage);
            if (string.IsNullOrEmpty(result.Url))
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل رفع صرة المنتج"));
            }
            
            var product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    ProductImageURl = result.Url,
                    ImagePublicId = result.PublicId,
                    Price = productDto.Price,
                    SupplierId = supplier.Id
                };
            await _unitOfWork.Products.AddAsync(product);
            existingSubscription.NumberOfProduct--;
            _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
            int saved = await _unitOfWork.SaveAsync();
            if (productDto.IsSpecial != null && productDto.IsSpecial != false)
            {
                var specialProduct = new SpecialProduct
                {
                    ProductId = product.Id,
                    Offer = productDto.Offer ?? 0
                };
                await _unitOfWork.SpecialProducts.AddAsync(specialProduct);
                existingSubscription.NumberOfSpecialProduct--;
                _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
                if (await _unitOfWork.SaveAsync() != 0)
                    return Ok(ResponseFactory.CreateMessageResponse("تم إضافة منتج مميز بنجاح "));
            }
            if (saved != 0)
            {
                return Ok(ResponseFactory.CreateMessageResponse("تم إضافة منتج بنجاح"));
            }
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل إضافة المنتج"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to delete an existing product. It verifies the product's ownership, deletes the associated image from cloud storage, and updates the supplier's subscription plan limits.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization & User/Product Validation:** The endpoint is restricted to users with the "Suppliers" role. It validates both the `userId` from the user's claims and the `productId` from the query string.
        /// 2. **Ownership Verification:** It retrieves the supplier and then attempts to find the product, ensuring that the product's `SupplierId` matches the authenticated supplier's ID. This is a critical security step to prevent a supplier from deleting another's product.
        /// 3. **Cloudinary Image Deletion:** If the product has an associated `ImagePublicId`, the endpoint calls the `_cloudinaryService` to delete the image from the cloud.
        /// 4. **Subscription Plan Update:** It retrieves the supplier's current subscription plan to update their product limits.
        /// 5. **Incrementing Limits:**
        ///    - It increments the `NumberOfProduct` count on the supplier's subscription plan, effectively "refunding" a product slot.
        ///    - If the product being deleted was a `SpecialProduct`, it also increments the `NumberOfSpecialProduct` count.
        /// 6. **Database Deletion:** The product entity is marked for deletion and the changes are saved asynchronously.
        /// 7. **Response:** A success message is returned if the deletion and all updates are successful. If the save operation fails, a `BadRequest` response is returned with an error message.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpDelete("supplier/product")]
        public async Task<IActionResult> DeleteProduct([FromQuery] int productId)
        {
            var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
            
            if (userId == null || userId <= 0 || productId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح id"));
            }
            
            var supplier = await _unitOfWork.Suppliers.FindAsync(k => k.UserId == userId, new[] { "User" });
            if (supplier == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("مورد غير صالح"));
            }
            var product = await _unitOfWork.Products.FindAsync(p => p.Id == productId && p.SupplierId == supplier.Id, new[] { "SpecialProduct" });
            if (product == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("منتج غير موجود"));
            }
            
            if (!string.IsNullOrEmpty(product.ImagePublicId))
            {
                await _cloudinaryService.DeleteAsync(product.ImagePublicId);
            }

            var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans.FindAsync(
                x => x.SupplierId == supplier.Id 
            );
            

            if (product.SpecialProduct != null)
            {
                existingSubscription.NumberOfSpecialProduct++;
            }
            existingSubscription.NumberOfProduct++;
            _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
            _unitOfWork.Products.Delete(product);
            if (await _unitOfWork.SaveAsync() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم حذف المنتج بنجاح"));
            
            return BadRequest(ResponseFactory.CreateMessageResponse(" فشل حذف المنتج حاول مرة أخرى"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint, accessible only to administrators, retrieves a paginated list of all pending supplier product requests. It allows the admin to review requests from suppliers who need to add more products to their plan.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is secured with the `[Authorize(Roles = "Admin")]` attribute, ensuring that only users with the "Admin" role can access it.
        /// 2. **Request Filtering:** It defines a search `criteria` to filter for requests with a `Status` of `Pending`.
        /// 3. **Pagination & Sorting:**
        ///    - It calculates the total number of pending requests to determine the total pages.
        ///    - It fetches a specific page of results using `FindWithFiltersAsync`, which applies the pending status filter.
        ///    - The results are sorted by their creation date (`CreatedAt`) in descending order to show the newest requests first.
        ///    - The pagination parameters (`page`, `pageSize`) are used to skip and take the correct number of records.
        /// 4. **Data Projection:** The raw supplier product request data is mapped to a `RequestToAddMoreDto` to create a clean, focused response. This DTO includes the `RequestId`, as well as the supplier's name, email, phone number, and the requested amount of products.
        /// 5. **Response:** It returns an `Ok` response containing a paginated object. This object includes the list of requests (`result`) and a `Meta` object with pagination details such as the `pageSize`, `currentPage`, `totalPages`, and `totalItems`.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/product-request")]
        public async Task<IActionResult> GetProductRequests(int page =1,int pageSize =10 )
        {

            Expression<Func<SupplierProductRequest, bool>> criteria = r => r.Status == Enum.RequestStatus.Pending;
            int totalItems = await _unitOfWork.SupplierProductRequests.CountAsync(criteria);
            
            var supplierProductRequests = await _unitOfWork.SupplierProductRequests.FindWithFiltersAsync(
                7,
                criteria: criteria,
                sortColumn: nameof(SupplierProductRequest.CreatedAt),
                sortColumnDirection: Enum.OrderBy.Desc.ToString(),
                skip: (page - 1) * pageSize,
                take: pageSize
            );
            var result = supplierProductRequests.Select(r => new RequestToAddMoreDto
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
        /// This endpoint allows an administrator to either accept or reject a pending supplier's request to add more products. Upon acceptance, the supplier's subscription plan is updated to reflect the new product limits.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization & Validation:** The endpoint is restricted to users with the "Admin" role. It validates that the `requestId` is a valid GUID and that the `operationType` is a valid enum value.
        /// 2. **Request Retrieval:** It searches for a pending `SupplierProductRequest` by its `requestId`. It also includes the associated supplier and their subscription plan in the query to avoid subsequent database calls.
        /// 3. **Processing:** If the request is found, its `ProcessedAt` timestamp is set. The code then checks the `operationType` to determine the next steps.
        /// 4. **Acceptance:** If the operation is "Accept", the request's status is changed to `Approved`. The `RequestedAmount` from the request is then added to both the `NumberOfProduct` and `NumberOfSpecialProduct` counts on the supplier's subscription plan.
        /// 5. **Rejection:** If the operation is not "Accept" (implying "Cancel"), the request's status is simply changed to `Rejected`.
        /// 6. **Finalization:** The updated request and subscription plan are saved to the database. A success message is returned if the save is successful; otherwise, a failure message is returned.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/accept-cancel-product-request")]
        public async Task<IActionResult> AdminAcceptOrCancelProductRequest(OperationOnRequestType operationType, Guid requestId)
        {
            if (requestId == Guid.Empty)
                return BadRequest(ResponseFactory.CreateMessageResponse("مطلوب إدخال رقم الطلب"));
            if (!System.Enum.IsDefined(typeof(OperationOnRequestType), operationType))
                return BadRequest(ResponseFactory.CreateMessageResponse("تم إدخال نوع عملية غير صالح"));
            SupplierProductRequest supplierProductRequest = await _unitOfWork.SupplierProductRequests.FindAsync(r => r.Id == requestId && r.Status == RequestStatus.Pending,
                ["Supplier" , "Supplier.SupplierSubscriptionPlan"]);
            bool check;
            if(supplierProductRequest is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("طلب غير موجود"));
            supplierProductRequest.ProcessedAt = DateTime.UtcNow;
            if(operationType == OperationOnRequestType.Accept)
            {
                supplierProductRequest.Status = RequestStatus.Approved;
                supplierProductRequest.Supplier.SupplierSubscriptionPlan.NumberOfSpecialProduct += supplierProductRequest.RequestedAmount;
                supplierProductRequest.Supplier.SupplierSubscriptionPlan.NumberOfProduct += supplierProductRequest.RequestedAmount;
                check =true;
            }
            else
            {
                supplierProductRequest.Status = RequestStatus.Rejected;
                check =false;
            }
            _unitOfWork.SupplierProductRequests.Update(supplierProductRequest);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse(check?"فشل قبول الطلب": "فشل رفض الطلب"));
            return Ok( ResponseFactory.CreateMessageResponse( check?"تم قبول الطلب بنجاح": "تم رفض الطلب بنجاح"));
        }

    }
}
