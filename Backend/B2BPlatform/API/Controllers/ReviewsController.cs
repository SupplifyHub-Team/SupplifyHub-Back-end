using API.DTO.Advertisment;
using API.DTO.GeneralResponse;
using API.DTO.Orders;
using API.DTO.Users;
using API.Factory;
using CloudinaryDotNet.Core;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Models.Entities;
using System.Linq.Expressions;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ReviewsController : APIBaseController
    {
        public ReviewsController(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated client to finalize an order and submit a review for a supplier. The process is handled within a database transaction to ensure all related operations—creating a deal, adding order details, and submitting a review—are completed successfully as a single atomic unit.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Clients" role.
        /// 2. **Input Validation:** It first validates the `ModelState` and the `clientId` from the user's claims. It then checks if the specified order has already been completed or canceled to prevent duplicate actions.
        /// 3. **Transaction Initiation:** The entire process is wrapped in a database transaction to guarantee data integrity.
        /// 4. **Order Status Update:** If the order's status is "Active", it is updated to "InProgress".
        /// 5. **Deal Creation:** A new `Deal` record is created, linking the client, the order, and the supplier. Its status is set to "ClientConfirmed".
        /// 6. **Order Details and Review:** After the deal is successfully added, the endpoint retrieves its ID. It then creates two new records:
        ///    - `DealDetailsVerification`: Stores details of the transaction such as the delivered date, price, and quantity.
        ///    - `Review`: Stores the client's rating and comments for the supplier.
        /// 7. **Database Operations:** The new `DealDetailsVerification` and `Review` records are added to the unit of work. The transaction is then committed to save all changes.
        /// 8. **Error Handling:** A `try-catch` block is used to manage exceptions. If any step within the transaction fails, a rollback is performed to revert all changes, ensuring the database remains in a consistent state.
        /// 9. **Response:**
        ///    - On success, it returns an `Ok` response with a confirmation message.
        ///    - On failure, it returns a `BadRequest` or `StatusCode(500)` with an appropriate error message.
        /// </summary>
        [Authorize(Roles = "Clients")]
        [HttpPost("client/review")]
        public async Task<IActionResult> AddOrderDetailsAndMakeReviewForClient([FromBody] AddDetailsOfOrderAndReviewForClient addDetailsOfOrderAndReview)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            if (!int.TryParse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value, out int clientId))
                return BadRequest("يوجد مشكله في Claims يرجى المحاولة فى وقت لاحق وإن لم تحل المشكلة يجب التوجة ألي فريق الدعم لحل المشكلة");
            Order order = await _unitOfWork.Orders.GetByIdAsync(addDetailsOfOrderAndReview.OrderId);
            if(order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Canceled)
                return BadRequest("هذة المناقصة بالفعل أكتملت أو تم ألغاءة");
            IDbContextTransaction transaction = null;
            try
            {
                transaction = await _unitOfWork.BeginTransactionAsync();
                Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == addDetailsOfOrderAndReview.UserId);
                if (order.OrderStatus == OrderStatus.Active)
                {
                    order.OrderStatus = OrderStatus.InProgress;
                    _unitOfWork.Orders.Update(order);
                }
                Deal deal = new()
                {
                    ClientId = clientId,
                    OrderId = addDetailsOfOrderAndReview.OrderId,
                    Status = DealStatus.ClientConfirmed,
                    SupplierId = supplier.Id,
                };
                await _unitOfWork.Deals.AddAsync(deal);
                await _unitOfWork.SaveAsync();
                Deal dealAfterAdd = await _unitOfWork.Deals.FindAsync(x => x.OrderId == deal.OrderId && x.ClientId == deal.ClientId && x.SupplierId == deal.SupplierId);
                DealDetailsVerification dealDetails = new()
                {
                    DealId = dealAfterAdd.Id,
                    DateOfDelivered = addDetailsOfOrderAndReview.DateOfDelivered.ToUniversalTime(),
                    //Price = addDetailsOfOrderAndReview.Price,
                    //DiscriptionAndQuantity= addDetailsOfOrderAndReview.DescriptionAndQuantity,
                    //Quantity = addDetailsOfOrderAndReview.Quantity,
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedById = clientId,
                    DealDoneAt = addDetailsOfOrderAndReview.DealDoneAt.ToUniversalTime(),
                    Items = new List<DealItem>()
                };
                foreach (var item in addDetailsOfOrderAndReview.Items)
                {
                    dealDetails.Items.Add(new DealItem
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }
                Review review = new()
                {
                    SubmittedAt = DateTime.UtcNow,
                    Comment = addDetailsOfOrderAndReview.Comment,
                    DealId = dealAfterAdd.Id,
                    Rating = addDetailsOfOrderAndReview.Rating,
                    ReviewerId = clientId,
                    RevieweeId = addDetailsOfOrderAndReview.UserId,
                };
                await _unitOfWork.DealDetailsVerifications.AddAsync(dealDetails);
                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Ok(ResponseFactory.CreateMessageResponse("تمت معالجة الطلب ومراجعته بنجاح"));
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await _unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, ResponseFactory.CreateMessageResponse("حدث خطأ أثناء معالجة الطلب"));
            }
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an authenticated supplier to finalize their part of a deal and submit a review for the client. The process is handled within a database transaction to ensure all related operations are completed as a single atomic unit.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Suppliers" role.
        /// 2. **Input Validation:** It first validates the `ModelState` and the `userId` from the user's claims. It then checks the status of the deal to ensure it is `ClientConfirmed`, preventing a supplier from adding details to a deal that has already been finalized by them. It also checks if the associated order has already been completed or canceled.
        /// 3. **Transaction Initiation:** The entire process is wrapped in a database transaction to guarantee data integrity.
        /// 4. **Deal Details & Review Creation:** The endpoint creates two new records:
        ///    - `DealDetailsVerification`: Stores the details of the transaction as provided by the supplier (e.g., delivered date, price, and description).
        ///    - `Review`: Stores the supplier's rating and comments for the client. The supplier is set as the reviewer and the client as the reviewee.
        /// 5. **Deal Status Update:** The deal's status is updated to `SupplierConfirmed`, signaling that the supplier has provided their confirmation of the deal details.
        /// 6. **Database Operations:** All changes—updating the deal and adding the new records—are saved to the database. The transaction is then committed.
        /// 7. **Error Handling:** A `try-catch` block is used to manage exceptions. If any step within the transaction fails, a rollback is performed to revert all changes, ensuring the database remains in a consistent state.
        /// 8. **Response:**
        ///    - On a successful operation, it returns an `Ok` response with a confirmation message.
        ///    - On failure (e.g., invalid input, database error), it returns a `BadRequest` or a `StatusCode(500)` with an appropriate error message.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/review")]
        public async Task<IActionResult> AddOrderDetailsAndMakeReviewForSupplier([FromBody] AddDetailsOfOrderAndReviewForSupplier addDetailsOfOrderAndReview)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            Deal deal = await _unitOfWork.Deals.FindAsync(s => s.Id == addDetailsOfOrderAndReview.DealId, new[] { "DealDetailsVerifications","Order" });
            if (deal.Status != DealStatus.ClientConfirmed)
                return Ok(ResponseFactory.CreateMessageResponse("لقد أدخلت بالفعل تفاصيل الصفقة وقمت بمراجعتها"));
            if (!int.TryParse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest("يوجد مشكله في Claims يرجى المحاولة فى وقت لاحق وإن لم تحل المشكلة يجب التوجة ألي فريق الدعم لحل المشكلة");
            if (deal.Order.OrderStatus == OrderStatus.Completed || deal.Order.OrderStatus == OrderStatus.Canceled)
                return BadRequest("هذة المناقصة بالفعل أكتملت أو تم ألغاءة");

            IDbContextTransaction transaction = null;
            try
            {
                transaction = await _unitOfWork.BeginTransactionAsync();
                DealDetailsVerification dealDetails = new()
                {
                    DealId = deal.Id,
                    DateOfDelivered = addDetailsOfOrderAndReview.DateOfDelivered.ToUniversalTime(),
                    //Price = addDetailsOfOrderAndReview.Price,
                    //DiscriptionAndQuantity= addDetailsOfOrderAndReview.DescriptionAndQuantity,
                    //Quantity = addDetailsOfOrderAndReview.Quantity,
                    DealDoneAt = addDetailsOfOrderAndReview.DealDoneAt.ToUniversalTime(),
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedById = userId,
                    Items = new List<DealItem>()
                };
                foreach (var item in addDetailsOfOrderAndReview.Items)
                {
                    dealDetails.Items.Add(new DealItem
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }
                Review review = new()
                {
                    SubmittedAt = DateTime.UtcNow,
                    Comment = addDetailsOfOrderAndReview.Comment,
                    DealId = deal.Id,
                    Rating = addDetailsOfOrderAndReview.Rating,
                    ReviewerId = userId,
                    RevieweeId = deal.ClientId
                };

                deal.Status = DealStatus.SupplierConfirmed;
                //DealDetailsVerification dealDetails1 = deal.DealDetailsVerifications.ElementAt(0);
                //if(dealDetails1.DealDoneAt == dealDetails.DealDoneAt 
                //    && dealDetails1.Quantity == dealDetails.Quantity 
                //    &&  dealDetails1.Price == dealDetails.Price 
                //    && dealDetails1.DateOfDelivered == dealDetails.DateOfDelivered)
                //    deal.Status = DealStatus.Confirmed;
                //else
                //    deal.Status= DealStatus.Unconfirmed;

                _unitOfWork.Deals.Update(deal);
                await _unitOfWork.DealDetailsVerifications.AddAsync(dealDetails);
                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();


                return Ok(ResponseFactory.CreateMessageResponse("تمت معالجة الطلب وإرسال المراجعة بنجاح"));
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                return StatusCode(500, ResponseFactory.CreateMessageResponse("حدث خطأ أثناء معالجة الطلب"));
            }
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a summary of ratings for a specific supplier, including the total number of ratings and the distribution of ratings across a 5-star scale.
        ///
        /// **Flow & Logic:**
        /// 1. **Input Validation:** The endpoint validates the `userId` provided in the route to ensure it's a valid user ID. It then attempts to find a supplier associated with that `userId`. If the supplier is not found, it returns a `BadRequest` response.
        /// 2. **Data Retrieval:** It fetches all reviews from the database where the `RevieweeId` matches the supplier's ID.
        /// 3. **Data Aggregation:** It initializes a `SummaryReviewDtO` object.
        ///    - It populates the `TotalRatings` field with the total count of the retrieved reviews.
        ///    - It iterates through each review and increments the count in the `Distribution` array based on the review's star rating (e.g., a 5-star rating increments the fifth element of the array). This provides a count of how many reviews fall into each star category (1-5).
        /// 4. **Response:** It returns an `Ok` response with the `SummaryReviewDtO` object, which contains the total number of ratings and the distribution counts.
        /// </summary>
        [HttpGet("ratings/summary/{userId:int}")]
        public async Task<IActionResult> GetSummaryReviews([FromRoute] int userId)
        {
            Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId);
            if (supplier == null)
                return BadRequest(ResponseFactory.CreateMessageResponse("المستخدم غير موجود"));
            var reviews = await _unitOfWork.Reviews.FindAllAsync(x=> x.RevieweeId == supplier.Id);
            SummaryReviewDtO summaryReviewDtO = new() 
            {
                TotalRatings = reviews.Count(),
            };
            foreach (var item in reviews)
                summaryReviewDtO.Distribution[item.Rating - 1]++;
            return Ok(ResponseFactory.CreateGeneralSingleResponse(summaryReviewDtO));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a paginated and sorted list of reviews for a specific supplier. It is designed to display detailed customer feedback, including comments and ratings, in a structured format.
        ///
        /// **Flow & Logic:**
        /// 1. **Input Validation:** The method accepts a `userId` from the URL, and optional `page` and `pageSize` parameters from the query string. It first validates the `userId` by attempting to find a corresponding supplier. If no supplier is found, it returns a `BadRequest` response.
        /// 2. **Review Filtering:** It defines a filter to retrieve all reviews where the `RevieweeId` matches the supplier's ID.
        /// 3. **Pagination & Sorting:**
        ///    - It calculates the total number of reviews that match the criteria.
        ///    - It uses `FindWithFiltersAsync` to fetch a specific page of reviews.
        ///    - The reviews are sorted in descending order (`Desc`) based on their `SubmittedAt` timestamp, ensuring the most recent reviews are returned first.
        /// 4. **Data Projection:** It maps the retrieved review entities to a `ReviewDto` object, which includes the `Comment`, `Rating`, and `ReviewerName`, simplifying the data structure for the response.
        /// 5. **Response:** It returns an `Ok` response containing a `PaginationResponse` object. This response includes both the paginated list of `ReviewDto`s and `Meta` information (total items, total pages, current page, etc.), which is useful for client-side pagination UI.
        /// </summary>
        [HttpGet("ratings/reviews/{userId:int}")]
        public async Task<IActionResult> GetReviews([FromRoute] int userId,
            int page = 1,
             int pageSize = 10
        )
        {
            Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s => s.UserId == userId);
            if (supplier == null)
                return BadRequest(ResponseFactory.CreateMessageResponse("المستخدم غير موجود"));
            Expression<Func<Review, bool>> criteria = r => r.RevieweeId == supplier.Id;
            int totalItems = await _unitOfWork.Reviews.CountAsync(criteria);
            // 4. Build the query
            var reviews = await _unitOfWork.Reviews.FindWithFiltersAsync(
                6,
                criteria: criteria,
                sortColumn:nameof(Review.SubmittedAt),
                sortColumnDirection: Enum.OrderBy.Desc.ToString(),
                skip: (page - 1) * pageSize,
                take: pageSize
            );
            var result = reviews.Select(r => new ReviewDto
            {
                Comment = r.Comment,
                Rating = r.Rating,
                ReviewerName = r.Reviewer.Name 
            }).ToList();
            return Ok(ResponseFactory.CreatePaginationResponse(result, new Meta
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize), // Fixed
                TotalItems = totalItems
            }));
        }
    
    }
}
