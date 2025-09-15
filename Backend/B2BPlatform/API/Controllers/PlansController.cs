using API.DTO;
using API.DTO.Plans;
using API.Factory;
using API.Services;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class PlansController : APIBaseController
    {
        private readonly IEmailService _emailService;
        public PlansController(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork) 
        {
            _emailService = emailService;
        }

       


        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an administrator to update the details of an existing subscription plan.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Input Validation:**
        ///    - It first checks if the provided `id` (plan ID) is valid (greater than 0).
        ///    - It then retrieves the plan from the database. If the plan doesn't exist, it returns an "Plan not found" message.
        ///    - It also performs model state validation to ensure the data received in the request body is valid.
        /// 3. **Plan Update:**
        ///    - The properties of the existing plan (`existingPlan`) are updated with the values from the `adminUpdatePlanDto`. This includes the name, price, duration, description, pros, and cons.
        ///    - The `UpdatedAt` timestamp is set to the current UTC time.
        /// 4. **Database Save:** The changes are saved to the database using `_unitOfWork.Save()`.
        /// 5. **Response:**
        ///    - If the save operation is successful, it returns an "Plan updated successfully" message.
        ///    - If the save operation fails, it returns a "Failed to update plan, try again" message.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/plans/{id}")]
        public async Task<IActionResult> AdminUpdatePlan([FromRoute] int id, [FromBody] AdminUpdatePlanDto adminUpdatePlanDto)
        {
            if (id <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح PlanId "));
            }
            var existingPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(id);
            if (existingPlan == null)
            {
                return Ok(ResponseFactory.CreateMessageResponse("خطة غير موجودة"));
            }

            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));

            existingPlan.Name = adminUpdatePlanDto.PlanName; //added
            existingPlan.Price = adminUpdatePlanDto.Price;
            existingPlan.Duration = adminUpdatePlanDto.Duration;
            existingPlan.Description = adminUpdatePlanDto.Description;
            existingPlan.UpdatedAt = DateTime.UtcNow;
            existingPlan.Pros = adminUpdatePlanDto.Pros;
            existingPlan.Cons = adminUpdatePlanDto.Cons;
            if (_unitOfWork.Save() != 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم تعديل الخطة بنجاح"));
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل تعديل الخطة حاول مرة أخرى"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a list of all subscription plans, ordered by price.
        ///
        /// **Flow & Logic:**
        /// 1. **Data Retrieval:** It asynchronously fetches all subscription plans from the database using `_unitOfWork.SubscriptionPlans.GetAllAsync()`.
        /// 2. **Data Processing:** The retrieved plans are ordered by their `Price` in ascending order.
        /// 3. **Data Mapping:** Each plan object is then mapped to a `PlanToShowDto`, which includes details such as `Id`, `PlanName`, `Price`, `Duration`, `Description`, and timestamps.
        /// 4. **Response:** The function returns a successful `Ok` response containing the list of `PlanToShowDto` objects. If no plans are found, it returns an empty list within the response.
        /// </summary>
        [HttpGet("plans")]
        public async Task<IActionResult> GetAllPlans()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync();
            //if (plans == null || !plans.Any())
            //{
            //    return Ok(ResponseFactory.CreateMessageResponse("No plans found."));
            //}
            var result = plans.OrderBy(p => p.Price).Select(p => new PlanToShowDto
            {
                Id = p.Id,
                PlanName = p.Name,
                Price = p.Price,
                Duration = p.Duration,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Pros = p.Pros,
                Cons = p.Cons
            }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(result));

        }

        
        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows a supplier to subscribe to a specific subscription plan.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Suppliers" role.
        /// 2. **Input Validation:**
        ///    - It validates the provided `planId`.
        ///    - It retrieves the subscription plan from the database. If not found, it returns an error message.
        /// 3. **User and Supplier Validation:**
        ///    - It extracts the `userId` from the user claims.
        ///    - It then retrieves the `Supplier` associated with the `userId`. If either is invalid or not found, an appropriate error message is returned.
        /// 4. **Subscription Logic (Plan ID 4):**
        ///    - A special condition is applied for `planId == 4`.
        ///    - It checks if the supplier already has an active subscription.
        ///    - If an existing subscription is found, it is first archived to `SupplierSubscriptionPlanArchive` and then updated with the new plan details.
        ///    - If no existing subscription is found, a new `SupplierSubscriptionPlan` is created and added to the database.
        ///    - The number of accepted orders is set to 30, and other features are set to false by default.
        ///    - The status is set to `PaymentStatus.Completed`.
        /// 5. **Subscription Logic (Other Plans):**
        ///    - For all other plans (not ID 4), the code checks if the supplier has a pending request for the same plan.
        ///    - If a pending request exists, it returns a message asking the supplier to wait for admin approval.
        ///    - If no pending request exists, a new `UnconfirmedSupplierSubscriptionPlan` is created and added to the database.
        ///    - This unconfirmed plan awaits admin confirmation.
        /// 6. **Database Save & Response:**
        ///    - The function attempts to save the changes to the database.
        ///    - It returns an appropriate success or failure message in Arabic based on the result of the save operation.
        /// </summary>
        [Authorize(Roles = "Suppliers")]
        [HttpPost("supplier/subscribe/{planId}")]
        public async Task<IActionResult> SubscribeToPlan([FromRoute] int planId)
        {
            if (planId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح Plan Id"));
            }

            var plan = await _unitOfWork.SubscriptionPlans.FindAsync(f => f.Id == planId);
            if (plan == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("خطة غير موجودة"));
            }

            var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
            if (userId == null || userId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("غير صالح user ID."));
            }

            var supplier = await _unitOfWork.Suppliers.FindAsync(u => u.UserId == userId, new[] { "User" });
            if (supplier == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("مورد غير موجود"));
            }

            if (plan.Id == 4)
            {
                var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans
                    .FindAsync(sp => sp.SupplierId == supplier.Id, new[] { "Supplier.User", "SubscriptionPlan" });
                if (existingSubscription != null)
                {
                    var archivedSubscription = new SupplierSubscriptionPlanArchive
                    {
                        SupplierId = existingSubscription.SupplierId,
                        PlanId = existingSubscription.PlanId,
                        PlanName = existingSubscription.SubscriptionPlan.Name,
                        PaymentStatus = existingSubscription.PaymentStatus,
                        NumberOfProduct = existingSubscription.NumberOfProduct,
                        NumberOfSpecialProduct = existingSubscription.NumberOfSpecialProduct,
                        NumberOfAdvertisement = existingSubscription.NumberOfAdvertisement,
                        NumberOfAcceptOrder = existingSubscription.NumberOfAcceptOrder,
                        StartDate = existingSubscription.StartDate,
                        EndDate = existingSubscription.EndDate,
                        ArchivedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.SupplierSubscriptionPlanArchives.AddAsync(archivedSubscription);
                    existingSubscription.PlanId = planId;
                    existingSubscription.PlanName = plan.Name;
                    existingSubscription.StartDate = DateTime.UtcNow;
                    existingSubscription.EndDate = DateTime.UtcNow.AddMonths(plan.Duration);
                    existingSubscription.NumberOfProduct = 0;
                    existingSubscription.NumberOfSpecialProduct = 0;
                    existingSubscription.NumberOfAdvertisement = 0;
                    existingSubscription.NumberOfAcceptOrder = 30;
                    existingSubscription.EarlyAccessToOrder = false;
                    existingSubscription.ShowHigherInSearch = false;
                    existingSubscription.CompetitorAndMarketAnalysis = false;
                    existingSubscription.ProductVisitsAndPerformanceAnalysis = false;
                    existingSubscription.DirectTechnicalSupport = false;
                    existingSubscription.PaymentStatus = PaymentStatus.Completed;
                    existingSubscription.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
                    if (await _unitOfWork.SaveAsync() > 0)
                    {
                        return Ok(ResponseFactory.CreateMessageResponse("تم الاشتراك بنجاح"));
                    }

                    return BadRequest(ResponseFactory.CreateMessageResponse("فشل الشتراك"));
                }
                else
                {
                    var newSubscription = new SupplierSubscriptionPlan
                    {
                        SupplierId = supplier.Id,
                        PlanId = planId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(plan.Duration),
                        PaymentStatus = PaymentStatus.Completed,
                        PlanName = plan.Name,
                        NumberOfProduct = 0,
                        NumberOfSpecialProduct = 0,
                        NumberOfAdvertisement = 0,
                        NumberOfAcceptOrder = 30,
                        EarlyAccessToOrder = false,
                        ShowHigherInSearch = false,
                        CompetitorAndMarketAnalysis = false,
                        ProductVisitsAndPerformanceAnalysis = false,
                        DirectTechnicalSupport = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.SupplierSubscriptionPlans.AddAsync(newSubscription);
                    if (await _unitOfWork.SaveAsync() > 0)
                    {
                        return Ok(ResponseFactory.CreateMessageResponse("تم طلب الاشتراك"));
                    }

                    return BadRequest(ResponseFactory.CreateMessageResponse("فشل الاشتراك"));
                }
            }

            // create unconfirmed supplier subscription plan
            var CheckThatNotMakeRequestInThatPlan= await _unitOfWork.UnconfirmedSupplierSubscriptionPlans
                .FindAsync(us => us.SupplierId == supplier.Id && us.PlanId == planId);

            if(CheckThatNotMakeRequestInThatPlan is not null)
                return Ok(ResponseFactory.CreateMessageResponse("أنت بالفعل قمت بتقديم طلب لهذة الخطة أنتظر فريق المنصة حتى يتواصل معك لتفعيل الأشتراك"));
            var unconfirmedSubscription = new UnconfirmedSupplierSubscriptionPlan
            {
                SupplierId = supplier.Id,
                PlanId = plan.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UnconfirmedSupplierSubscriptionPlans.AddAsync(unconfirmedSubscription);

            if (await _unitOfWork.SaveAsync() > 0)
            {
                return Ok(ResponseFactory.CreateMessageResponse("تم طلب الاشتراك"));
            }

            return BadRequest(ResponseFactory.CreateMessageResponse("فشل الاشتراك"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an administrator to retrieve a list of all unconfirmed supplier subscription requests that are pending approval.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Data Retrieval:** It asynchronously finds all records in the `UnconfirmedSupplierSubscriptionPlans` table, including related data for the `Supplier` (and their `User` details) and the `SubscriptionPlan`.
        /// 3. **Data Check:** It checks if the retrieved list is empty or null. If so, it returns an empty response to the administrator.
        /// 4. **Data Mapping:** The retrieved unconfirmed subscriptions are mapped to `UnconfirmedSubscriptionDto` objects. This DTO includes key details such as the plan ID, supplier ID, supplier's name and email, plan name, plan duration, the date the request was created, and the supplier's join date.
        /// 5. **Response:** It returns an `Ok` response containing the list of `UnconfirmedSubscriptionDto` objects, which the admin can then use to review and accept or deny the requests.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/subscriptions-to-accept")]
        public async Task<IActionResult> GetSubscriptionsToAccept()
        {
            var unconfirmedSubscriptions = await _unitOfWork.UnconfirmedSupplierSubscriptionPlans.FindAllAsyncWithoutCraiteria(new[] { "Supplier.User", "SubscriptionPlan" });
            if (unconfirmedSubscriptions == null || !unconfirmedSubscriptions.Any())
            {
                return Ok(ResponseFactory.CreateGeneralSingleResponse(new List<int>()));
            }
            var result = unconfirmedSubscriptions.Select(us => new UnconfirmedSubscriptionDto
            {
                PlanId = us.PlanId,
                SupplierId = us.SupplierId,
                SupplierName = us.Supplier.User.Name,
                Email = us.Supplier.User.Email,
                PlanName = us.SubscriptionPlan.Name,
                Duration = us.SubscriptionPlan.Duration,
                CreatedAt = us.CreatedAt,
                JoinDate = us.Supplier.User.CreatedAt
            }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(result));

        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an administrator to confirm a supplier's subscription request. It activates the new plan and updates or creates a subscription record for the supplier.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Input Validation:** It validates that the `supplierId` and `planId` are valid positive integers.
        /// 3. **Request Retrieval:** It finds the pending subscription request in the `UnconfirmedSupplierSubscriptionPlans` table. If the request does not exist, it returns an error.
        /// 4. **Feature Assignment:** A `switch` statement is used to assign specific feature limits and access (e.g., `numberOfProduct`, `earlyAccessToOrder`) based on the confirmed `PlanId`. An error is returned if the plan ID is not supported.
        /// 5. **Subscription Update/Creation:**
        ///    - It checks if the supplier already has an existing active subscription.
        ///    - **If a subscription exists:** The existing subscription is first archived to `SupplierSubscriptionPlanArchive` and then updated with the new plan's details, features, and an end date.
        ///    - **If no subscription exists:** A new `SupplierSubscriptionPlan` record is created for the supplier with the confirmed plan's features.
        /// 6. **Cleanup:** The original unconfirmed subscription request is deleted from the database.
        /// 7. **Database Save & Response:** The changes are saved to the database. A success message is returned if the save operation is successful; otherwise, a failure message is returned.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/subscriptions/{supplierId}/{planId}")]
        public async Task<IActionResult> ConfirmSubscription([FromRoute] int supplierId, [FromRoute] int planId)
        {
            if (supplierId <= 0 || planId <= 0)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse(" غير صالح supplier ID أو plan ID."));
            }
            var unconfirmedSubscription = await _unitOfWork.UnconfirmedSupplierSubscriptionPlans
                .FindAsync(us => us.SupplierId == supplierId && us.PlanId == planId, new[] { "Supplier.User", "SubscriptionPlan" });

            if (unconfirmedSubscription == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("طلب اشتراك غير موجود"));
            }

            int numberOfProduct;
            int numberOfSpecialProduct;
            int numberOfAdvertisement;
            int numberOfAcceptOrder;
            bool earlyAccessToOrder;
            bool showHigherInSearch;
            bool competitorAndMarketAnalysis;
            bool productVisitsAndPerformanceAnalysis;
            bool directTechnicalSupport;

            switch (unconfirmedSubscription.PlanId)
            {
                case 5: 
                    numberOfProduct = 100;
                    numberOfSpecialProduct = 5;
                    numberOfAdvertisement = 5;
                    numberOfAcceptOrder = int.MaxValue;
                    earlyAccessToOrder = true;
                    showHigherInSearch = true;
                    competitorAndMarketAnalysis = true;
                    productVisitsAndPerformanceAnalysis = true;
                    directTechnicalSupport = true;
                    break;
                case 6: 
                    numberOfProduct = 25;
                    numberOfSpecialProduct = 3;
                    numberOfAdvertisement = 1;
                    numberOfAcceptOrder = int.MaxValue;
                    earlyAccessToOrder = false;
                    showHigherInSearch = false;
                    competitorAndMarketAnalysis = false;
                    productVisitsAndPerformanceAnalysis = false;
                    directTechnicalSupport = true;
                    break;
                default:
                    return BadRequest(ResponseFactory.CreateMessageResponse("مميزات الخطة غير صحيحة"));
            }
            // Check if the supplier already has an active subscription
            var existingSubscription = await _unitOfWork.SupplierSubscriptionPlans
                .FindAsync(sp => sp.SupplierId == supplierId, new[] { "Supplier.User", "SubscriptionPlan" });
            
            SupplierSubscriptionPlan finalSubscription;

            if (existingSubscription != null)
            {
                var archivedSubscription = new SupplierSubscriptionPlanArchive
                {
                    SupplierId = existingSubscription.SupplierId,
                    PlanId = existingSubscription.PlanId,
                    PlanName = existingSubscription.SubscriptionPlan.Name,
                    PaymentStatus = existingSubscription.PaymentStatus,
                    NumberOfProduct = existingSubscription.NumberOfProduct,
                    NumberOfSpecialProduct = existingSubscription.NumberOfSpecialProduct,
                    NumberOfAdvertisement = existingSubscription.NumberOfAdvertisement,
                    NumberOfAcceptOrder = existingSubscription.NumberOfAcceptOrder,
                    StartDate = existingSubscription.StartDate,
                    EndDate = existingSubscription.EndDate,
                    ArchivedAt = DateTime.UtcNow

                };
                await _unitOfWork.SupplierSubscriptionPlanArchives.AddAsync(archivedSubscription);
                existingSubscription.PlanId = unconfirmedSubscription.PlanId;
                existingSubscription.PlanName = unconfirmedSubscription.SubscriptionPlan.Name;
                existingSubscription.StartDate = DateTime.UtcNow;
                existingSubscription.EndDate = DateTime.UtcNow.AddMonths(unconfirmedSubscription.SubscriptionPlan.Duration);
                existingSubscription.NumberOfProduct = numberOfProduct;
                existingSubscription.NumberOfSpecialProduct = numberOfSpecialProduct;
                existingSubscription.NumberOfAdvertisement = numberOfAdvertisement;
                existingSubscription.NumberOfAcceptOrder = numberOfAcceptOrder;
                existingSubscription.EarlyAccessToOrder = earlyAccessToOrder;
                existingSubscription.ShowHigherInSearch = showHigherInSearch;
                existingSubscription.CompetitorAndMarketAnalysis = competitorAndMarketAnalysis;
                existingSubscription.ProductVisitsAndPerformanceAnalysis = productVisitsAndPerformanceAnalysis;
                existingSubscription.DirectTechnicalSupport = directTechnicalSupport;
                existingSubscription.PaymentStatus = PaymentStatus.Completed;
                existingSubscription.UpdatedAt = DateTime.UtcNow;

                // not completed yet

                //if (numberOfProduct < existingSubscription.NumberOfProduct)
                //{
                //    var activeProducts = _unitOfWork.Products
                //        .FindAllAsync(p => p.SupplierId == supplierId && p.Status == ProductStatus.Active)
                //        .OrderByDescending(p => p.Id) // Deactivate oldest products first.
                //        .ToList();

                //    var productsToDeactivate = activeProducts
                //        .Skip(planProductLimits[newPlan])
                //        .ToList();
                //    foreach (var product in productsToDeactivate)
                //    {
                //        product.Status = ProductStatus.Deactivated;
                //    }
                //    _dbContext.SaveChanges();
                //}
                _unitOfWork.SupplierSubscriptionPlans.Update(existingSubscription);
                finalSubscription = existingSubscription;
            }
            else
            {
                var newSubscription = new SupplierSubscriptionPlan
                {
                    SupplierId = supplierId,
                    PlanId = unconfirmedSubscription.PlanId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(unconfirmedSubscription.SubscriptionPlan.Duration),
                    PaymentStatus = PaymentStatus.Completed,
                    PlanName = unconfirmedSubscription.SubscriptionPlan.Name,
                    NumberOfProduct = numberOfProduct,
                    NumberOfSpecialProduct = numberOfSpecialProduct,
                    NumberOfAdvertisement = numberOfAdvertisement,
                    NumberOfAcceptOrder = numberOfAcceptOrder,
                    EarlyAccessToOrder = earlyAccessToOrder,
                    ShowHigherInSearch = showHigherInSearch,
                    CompetitorAndMarketAnalysis = competitorAndMarketAnalysis,
                    ProductVisitsAndPerformanceAnalysis = productVisitsAndPerformanceAnalysis,
                    DirectTechnicalSupport = directTechnicalSupport,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.SupplierSubscriptionPlans.AddAsync(newSubscription);
                finalSubscription = newSubscription;
            }

            _unitOfWork.UnconfirmedSupplierSubscriptionPlans.Delete(unconfirmedSubscription);


            if (await _unitOfWork.SaveAsync() > 0)
            {
                var notificationEmail = new EmailDto()
                {
                    To = finalSubscription.Supplier.User.Email,
                    Subject = "تهانينا! تم قبول طلب تسجيلك - منصة SuppliFy",
                    Body = $@"
        <div style=""font-family: 'Tajawal', sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
            <h2 style=""color: #28a745; text-align: center;""> تهانينا! تم قبول طلبك</h2>
            <p><strong>مرحباً {finalSubscription.Supplier.User.Name}،</strong></p>
            <p>يسعدنا أن نبلغك بأنه قد تم <strong>الموافقة على طلب تسجيلك</strong> في منصة <strong>SupplyFi</strong>.</p>
            <p>نحن متحمسون لانضمامك إلى شبكتنا ونتطلع إلى تعاون مثمر معك.</p>
            <p>يمكنك الآن تسجيل الدخول إلى حسابك والبدء في الاستفادة من جميع خدمات المنصة.</p>
            <p>إذا كنت بحاجة إلى أي مساعدة، لا تتردد في التواصل معنا عبر البريد الإلكتروني.</p>
            <p>أهلاً وسهلاً بك معنا، ونتمنى لك تجربة ناجحة ومفيدة </p>
            <p><strong>فريق منصة SuppliFy</strong></p>
            <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
            <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
        </div>
        "
                };
                _emailService.SendEmail(notificationEmail);

                return Ok(ResponseFactory.CreateMessageResponse("تم تأكيد الاشتراك بنجاح و إرسال البريد الالكتروني"));
            }
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل تأكيد الاشتراك"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an administrator to delete a pending, unconfirmed supplier subscription request.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Input Validation:** It takes `supplierId` and `planId` from the route parameters.
        /// 3. **Request Retrieval:** It searches for a matching unconfirmed subscription record in the database using the provided IDs.
        /// 4. **Deletion:** If the unconfirmed subscription is found, it is marked for deletion. If not found, it returns an error message in Arabic indicating that the subscription does not exist.
        /// 5. **Database Save & Response:** The function attempts to save the changes to the database. A success message in Arabic is returned if the deletion is successful, otherwise, an Arabic error message is returned indicating that the deletion failed.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/subscriptions/{supplierId}/{planId}")]
        public async Task<IActionResult> DeleteSubscription([FromRoute] int supplierId, [FromRoute] int planId)
        {
            var unconfirmedSubscription = await _unitOfWork.UnconfirmedSupplierSubscriptionPlans
                .FindAsync(s => s.SupplierId == supplierId && s.PlanId == planId, new[] { "Supplier.User", "SubscriptionPlan" });

            if (unconfirmedSubscription == null)
            {
                return BadRequest(ResponseFactory.CreateMessageResponse("الاشتراك غير موجود"));
            }

            _unitOfWork.UnconfirmedSupplierSubscriptionPlans.Delete(unconfirmedSubscription);
            
                if (await _unitOfWork.SaveAsync() > 0)
                {
                    var notificationEmail = new EmailDto()
                    {
                        To = unconfirmedSubscription.Supplier.User.Email,
                        Subject = "بخصوص طلب الاشتراك الخاص بك - منصة SuppliFy",
                        Body = $@"
<div style=""font-family: 'Tajawal', sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; text-align: right; direction: rtl;"">
    <h2 style=""color: #dc3545; text-align: center;"">نأسف، لم يتم قبول طلب الاشتراك</h2>
    <p><strong>مرحباً {unconfirmedSubscription.Supplier.User.Name}،</strong></p>
    <p>بعد مراجعة طلبك للاشتراك في الخطة <strong>{unconfirmedSubscription.SubscriptionPlan.Name}</strong>، نأسف لإبلاغك بأنه لم يتم قبول هذا الطلب.</p>
    <p>قد يكون السبب عدم اكتمال بعض البيانات أو عدم توافق الاشتراك مع متطلبات المنصة.</p>
    <p>يمكنك التقديم مرة أخرى بعد مراجعة بياناتك والتأكد من استيفاء جميع الشروط.</p>
    <p>إذا كانت لديك أي استفسارات أو كنت بحاجة إلى مساعدة، يمكنك التواصل معنا مباشرة عبر البريد الإلكتروني.</p>
    <p>نتمنى لك التوفيق،</p>
    <p><strong>فريق منصة SuppliFy</strong></p>
    <hr style=""border: 0; border-top: 1px solid #eee; margin: 20px 0;"">
    <p style=""font-size: 0.8em; color: #888; text-align: center;"">هذه الرسالة تم إنشاؤها تلقائياً. يرجى عدم الرد عليها.</p>
</div>
"
                    };
                    _emailService.SendEmail(notificationEmail);

                    return Ok(ResponseFactory.CreateMessageResponse("تم حذف الاشتراك وإرسال إشعار بالبريد الإلكتروني"));
                
            }
            return BadRequest(ResponseFactory.CreateMessageResponse("فشل حذف الاشتراك"));
        }

        //[Authorize(Roles = "Admin")]
        //[HttpPost("admin/plans")]
        //public async Task<IActionResult> AdminAddPlan([FromBody] AdminAddPlanDto adminAddPlanDto)
        //{
        //    if (adminAddPlanDto == null || string.IsNullOrWhiteSpace(adminAddPlanDto.PlanName))
        //    {
        //        return BadRequest(ResponseFactory.CreateMessageResponse("Invalid plan data."));
        //    }
        //    var existingPlan = await _unitOfWork.SubscriptionPlans.GetByColumnAsync("PlanName", adminAddPlanDto.PlanName);
        //    if (existingPlan != null)
        //    {
        //        return Conflict(ResponseFactory.CreateMessageResponse("Plan already exists."));
        //    }
        //    var newPlan = new SubscriptionPlan
        //    {
        //        Name = adminAddPlanDto.PlanName,
        //        Price = adminAddPlanDto.Price,
        //        Duration = adminAddPlanDto.Duration,
        //        Description = adminAddPlanDto.Description,
        //        CreatedAt = DateTime.UtcNow,
        //        Cons = adminAddPlanDto.Cons,
        //        Pros = adminAddPlanDto.Pros
        //    };
        //    _unitOfWork.SubscriptionPlans.Add(newPlan);
        //    if (_unitOfWork.Save() != 0)
        //        return Ok(ResponseFactory.CreateMessageResponse("Plan added successfully."));
        //    return BadRequest(ResponseFactory.CreateMessageResponse("Failed to add plan."));
        //}



        //[Authorize(Roles = "Admin")]
        //[HttpDelete("admin/plans/{id}")]
        //public async Task<IActionResult> AdminDeletePlan([FromRoute] int id)
        //{
        //    if (id <= 0)
        //    {
        //        return BadRequest(ResponseFactory.CreateMessageResponse("Invalid plan ID."));
        //    }
        //    var existingPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(id);
        //    if (existingPlan == null)
        //    {
        //        return NotFound("Plan not found.");
        //    }
        //    _unitOfWork.SubscriptionPlans.Delete(existingPlan);
        //    if (_unitOfWork.Save() != 0)
        //        return Ok(ResponseFactory.CreateMessageResponse("Plan deleted successfully."));
        //    return BadRequest(ResponseFactory.CreateMessageResponse("Failed to delete plan."));
        //}

    }
}
