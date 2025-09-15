using API.DTO;
using API.DTO.GeneralResponse;
using API.DTO.Orders;
using API.Factory;
using API.Services;
using Controllers;
using Entities;
using Enum;
using Hangfire;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data; 
using Microsoft.AspNetCore.Mvc;
using Models.Entities;
using Org.BouncyCastle.Asn1.Ocsp;
using SixLabors.ImageSharp.Processing;
using System.Linq.Expressions;
using System.Numerics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
// Add these using statements at the top of your controller
using System.Text.Json;


namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class OrdersController : APIBaseController
    {
        private readonly IEmailService _emailService;
        private readonly TokenService _tokenService;
        public OrdersController(TokenService tokenService, IUnitOfWork unitOfWork, IEmailService _emailService, IConfiguration _config) : base(unitOfWork, _config)
        {
            this._emailService = _emailService;
            _tokenService = tokenService;
        }

        /// <summary>
        /// **Function Summary:**
        /// This method retrieves a paginated and filtered list of all orders for an administrator.
        ///
        /// It allows for filtering based on a search term (client name or email),
        /// order category, and order status. The results are paginated and can be
        /// sorted by a specified column and direction.
        /// The method validates the provided parameters,
        /// constructs a dynamic filter query, and fetches the matching orders.
        /// It then maps the order details to a data transfer object and returns the
        /// results along with pagination metadata.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/orders")]
        public async Task<IActionResult> GetAllOrdersWithFilters(
        string? search,
        int? category,
        string? status,
        int page = 1,
        int pageSize = 10,
        string sortColumn = "Deadline",
        string sortColumnDirection = "Desc")
        {

            if (category != null)
            {
                var categoryEntity = await _unitOfWork.Categories.FindAsync(c => c.Id == category);
                if (categoryEntity == null)
                {
                    return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize, page));
                }

            }
            OrderBy orderBy = default;
            bool statusIsValid = true;
            if (!string.IsNullOrEmpty(sortColumnDirection))
                statusIsValid = System.Enum.TryParse<OrderBy>(sortColumnDirection, true, out orderBy);
            if (!statusIsValid)
                return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize, page));
            OrderStatus? orderStatus = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (!System.Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                    return Ok(ResponseFactory.CreatePaginationEmptyResponse(pageSize, page));
                orderStatus = parsedStatus;
            }
            // Get orders with related company and category
            // Define filter criteria (reuse for both count and data)
            Expression<Func<Order, bool>> criteria = o =>
                o.User != null &&
                (string.IsNullOrEmpty(search) ||
                    o.User.Name.ToLower().Contains(search.ToLower()) ||
                    o.User.Email.Contains(search)) &&
                (category == null ||
                    o.CategoryId == category) &&
                (orderStatus == null || o.OrderStatus == orderStatus);

            // ADD THIS LINE: Get total count of matching records
            int totalCount = await _unitOfWork.Orders.CountAsync(criteria);
            var orders = await _unitOfWork.Orders.FindWithFiltersAsync(
                2,
                criteria: criteria,
                sortColumn: sortColumn,
                sortColumnDirection: orderBy.ToString(),
                skip: (page - 1) * pageSize,
                take: pageSize
            );
            var result = orders.Select(order => new ClientOrderDto
            {
                OrderId = order.Id,
                Name = order.User.Name,
                OfferNumbers =order.NumSuppliersDesired,
                Email = order.User.Email,
                Category = _unitOfWork.Categories.GetById(order.CategoryId)?.Name ?? "Unknown",
                Items = order.Items.Select(i => new OrderItemToShowDto
                {
                    ItemId = i.Id,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Notes = i.Notes
                }).ToList(),
                CreatedAt = order.CreatedAt,
                OrderStatus = order.OrderStatus.ToString(),
                Deadline = order.Deadline
            }).ToList();
            return Ok(ResponseFactory.CreatePaginationResponse(result, new Meta
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize), // Fixed
                TotalItems = totalCount // Fixed
            }));
        }

        /// <summary>
        /// **Function Summary:**
        /// This function is used to create a new "tender" (purchase order) by the client user.
        ///
        /// The function first validates the input data and extracts the user ID from their claims.
        /// It then creates a new order in the database.
        /// If the order is successfully saved, the function searches for all active suppliers
        /// who have a confirmed subscription in the same order category.
        ///
        /// The function sends an email notification to each supplier,
        /// where the notification is sent immediately to suppliers with the early access feature,
        /// while it is scheduled for one hour later for other suppliers.
        ///
        /// The function returns a success message upon completion or an error message if there is a
        /// problem saving the order or sending the emails.
        /// </summary>
        [Authorize]
        [HttpPost("order")]
        public async Task<IActionResult> CreateTender(AddOrderDto tenderData)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));

            if (!int.TryParse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("there is problem in claims"));

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            // Create new order
            var order = new Order
            {
                ContactPersonNumber = tenderData.ContactPersonPhone,
                ContactPersonName = tenderData.ContactPersonName,
                RequiredLocation = tenderData.RequiredLocation,
                NumSuppliersDesired = tenderData.NumSuppliersDesired,
                Deadline = tenderData.Deadline.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow,
                OrderStatus = OrderStatus.Active,
                CategoryId = tenderData.CategoryId,
                UserId = user.Id,
                Items = tenderData.Items.Select(i => new OrderItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Notes = i.Notes

                }).ToList()
            };

            await _unitOfWork.Orders.AddAsync(order);

            if (await _unitOfWork.SaveAsync() != 0)
            {
                var users = await _unitOfWork.Users.FindAllAsync(
                    u => u.UserRoles.Any(ur => ur.Role.Name == RoleName.Suppliers) &&
                    u.Supplier.IsConfirmByAdmin == true &&
                    u.EmailVerified == true &&
                    u.IsActive == true &&
                    u.Supplier.SupplierCategories.Any(cc => cc.CategoryId == order.CategoryId),
                    includes: new[] { "Supplier", "Supplier.SupplierCategories", "Supplier.SupplierSubscriptionPlan" }
                );

                try
                {
                    var itemsHtml = string.Join("", order.Items.Select(i =>
    $@"<tr>
        <td style='padding:8px; border:1px solid #ddd;'>{i.Name}</td>
        <td style='padding:8px; border:1px solid #ddd; text-align:center;'>{i.Quantity}</td>
        <td style='padding:8px; border:1px solid #ddd;'>{i.Notes ?? "-"}</td>
      </tr>"
));
                    var category = await _unitOfWork.Categories.GetByIdAsync(order.CategoryId);
                    foreach (var userToSend in users)
                    {
                        // Generate secure one-time token
                        var secureToken = _tokenService.GenerateSecureToken(order.Id, userToSend.Id, order.Deadline);
                        var contactRequestLink = $"https://b2bapp.runasp.net/api/request-contact?orderId={order.Id}&token={secureToken}";

                        var emailDto = new EmailDto
                        {
                            To = userToSend.Email,
                            Subject = $"🚀 مناقصة جديدة متاحة - {category.Name}",
                            Body = $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f4f6f8; padding:20px; direction:rtl; text-align:right;'>
  <div style='max-width:650px; margin:0 auto; background:#fff; border:1px solid #ddd; border-radius:10px; padding:25px;'>
    
    <h2 style='color:#007bff; text-align:center; margin-bottom:20px;'>🚀 مناقصة جديدة متاحة</h2>
    <p style='font-size:15px;'>هناك مناقصة جديدة في فئة <strong>{category.Name}</strong> مطابقة لفئة عملك:</p>

    <!-- تفاصيل المنتجات -->
    <h3 style='margin-top:20px; color:#333;'> تفاصيل المنتجات المطلوبة:</h3>
    <table style='width:100%; border-collapse:collapse; margin-top:10px; font-size:14px;'>
      <thead style='background:#f1f1f1;'>
        <tr>
          <th style='padding:10px; border:1px solid #ddd;'>المنتج</th>
          <th style='padding:10px; border:1px solid #ddd;'>الكمية</th>
          <th style='padding:10px; border:1px solid #ddd;'>ملاحظات</th>
        </tr>
      </thead>
      <tbody>
        {itemsHtml}
      </tbody>
    </table>

    <!-- بيانات إضافية -->
    <h3 style='margin-top:25px; color:#333;'>📍 بيانات إضافية:</h3>
    <ul style='line-height:1.8; font-size:14px;'>
      <li><strong>المكان:</strong> {order.RequiredLocation}</li>
      <li><strong>آخر موعد للتقديم:</strong> {order.Deadline:dd MMM yyyy}</li>
    </ul>

    <!-- زر CTA -->
    <div style='text-align:center; margin:30px 0;'>
      <a href='{contactRequestLink}' style='background:#007bff; color:#fff; padding:14px 28px; text-decoration:none; border-radius:6px; font-weight:bold; font-size:15px;'>
        📩 طلب بيانات التواصل
      </a>
    </div>

    <p style='margin-top:25px; font-size:14px;'>مع أطيب التحيات،<br><strong>فريق SuppliFy</strong></p>

    <!-- ملاحظة -->
    <div style='margin-top:25px; padding:12px; border-top:1px solid #eee; color:#777; font-size:13px; background:#fafafa;'>
      <p><strong>ملاحظة:</strong> الرابط يُستخدم مرة واحدة فقط وينتهي صلاحيته بتاريخ {order.Deadline:dd MMM yyyy}</p>
    </div>
  </div>
</body>
</html>"
                        };

                        if (userToSend.Supplier.SupplierSubscriptionPlan.EarlyAccessToOrder)
                            _emailService.SendEmail(emailDto);
                        else
                        {
                            BackgroundJob.Schedule(() =>
                                _emailService.SendEmail(emailDto),
                                TimeSpan.FromHours(1));
                        }
                    }
                    return Ok(ResponseFactory.CreateMessageResponse("تم عمل المناقصة بنجاح"));
                }
                catch (Exception ex)
                {
                    return BadRequest(ResponseFactory.CreateMessageResponse("تم عمل المناقصة ولكن لم يتم ارسال الإيميلات قم بعمل مناقصة أخرة إن لم تحل المشكلة توجة ألي فريق الدعم لمساعدتك"));
                }
            }

            return BadRequest(ResponseFactory.CreateMessageResponse("فشل في عمل المناقصة قم بالمحاولة مره أخرة إن لم تنجح توجة ألي فريق الدعم"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows a supplier to use a secure, one-time token to request contact information
        /// for a specific order. The token is typically received via an email notification.
        ///
        /// **Flow & Logic:**
        /// 1. **Initial Validation:** The method first validates the `orderId` and `token` parameters.
        /// 2. **Token and Order Integrity Check:** It validates the secure token to ensure it is valid,
        ///    has not been tampered with, and matches the provided order ID.
        /// 3. **Order Status Check:** It checks if the order is still open and has not been
        ///    canceled, completed, or had its deadline expire.
        /// 4. **Supplier Subscription Check:** It verifies if the supplier's subscription plan has
        ///    remaining 'accepted order' slots. If not, it returns a specific message to the user.
        /// 5. **Successful Request:** If all checks pass, the system decrements the number of
        ///    desired suppliers for the order and the number of available accepted orders for the supplier.
        /// 6. **Contact Information Delivery:** An email containing the client's contact details is sent to the supplier.
        /// 7. **Response:** The endpoint returns a static HTML page to the user's browser,
        ///    informing them of the outcome and that the link is no longer valid.
        ///
        /// The method returns a custom HTML page for various error conditions, such as an
        /// invalid link, an expired link, or a completed order.
        /// </summary>
        [HttpGet("request-contact")]
        public async Task<IActionResult> RequestContact([FromQuery] int orderId, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return Content(@"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هذا الرابط تم  التلاعب بيه يرجى عدم التلاعب  بالرابط مرة أخرى أو سيتم حذف حسابك علي الموقع.</p>
                              </div>
                            </body>
                            </html>", "text/html");
            if (orderId <= 0)
                return Content(@"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هذا الرابط تم  التلاعب بيه يرجى عدم التلاعب  بالرابط مرة أخرى أو سيتم حذف حسابك علي الموقع.</p>
                              </div>
                            </body>
                            </html>", "text/html");

            // Validate the secure token
            var tokenData = _tokenService.ValidateAndParseToken(token);
            if (tokenData == null)
                return Content(@"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هذا الرابط إما منتهي الصلاحية أو تم استخدامه بالفعل.</p>
                              </div>
                            </body>
                            </html>", "text/html");
            // Verify token matches the order
            if (tokenData.OrderId != orderId)
                return Content(@"
                         <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هذا الرابط تم  التلاعب بيه يرجى عدم التلاعب بالرابط مرة أخرى أو سيتم حذف حسابك علي الموقع.</p>
                              </div>
                            </body>
                            </html>", "text/html");
            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, includes: new[] { "User" });
            if (order == null)
                return Content(@"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هناك مشكلة في قبول المناقصة يجب التوجه ألي فريق الدعم</p>
                              </div>
                            </body>
                            </html>", "text/html");
            var user = await _unitOfWork.Users.FindAsync(u => u.Id == tokenData.UserId, ["Supplier", "Supplier.SupplierSubscriptionPlan"]);
            if (user == null)
                return Content(@"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                            <body style='font-family: Arial, sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                              <div style='max-width:600px; margin:0 auto; background:#fff; padding:20px; border-radius:8px; text-align:center;'>
                                <h2 style='color:#dc3545;'> الرابط غير صالح</h2>
                                <p>هناك مشكلة في حسابك يجب التوجة ألي الدعم الفنى</p>
                              </div>
                            </body>
                            </html>", "text/html");
            var contactEmail = new EmailDto
            {
                To = user.Email,
                Subject = $" بيانات التواصل الخاصة بالطلب ",
            };

            string Body;
            bool checkToUpdateOrderStatus = false;

            if (order.NumSuppliersDesired < 1)
            {
                Body = $@"
                        <html>
                        <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                          <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px;'>
                            <h2 style='color:#28a745;'> إشعار بخصوص المناقصة</h2>
                            <p>تم إكمال الطلب من خلال موردين آخرين.</p>
                            <p>نشكرك على اهتمامك ونشجعك على متابعة الفرص الجديدة على منصتنا.</p>
                            <p>مع تحياتنا،<br><strong>فريق SuppliFy</strong></p>
                          </div>
                        </body>
                        </html>";
                checkToUpdateOrderStatus = true;
            }
            else if (order.OrderStatus == OrderStatus.Canceled)
            {
                Body = $@"
                    <html>
                    <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                      <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px;'>
                        <h2 style='color:#dc3545;'> تم إلغاء الطلب</h2>
                        <p>قام العميل بإلغاء الطلب </p>
                        <p>نشكرك على اهتمامك ونشجعك على متابعة الفرص الجديدة على منصتنا.</p>
                        <p>مع تحياتنا،<br><strong>فريق SuppliFy</strong></p>
                      </div>
                    </body>
                    </html>";
            }
            else if (order.OrderStatus == OrderStatus.Completed)
            {
                Body = $@"
                    <html>
                    <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                      <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px;'>
                        <h2 style='color:#17a2b8;'> الطلب مكتمل</h2>
                        <p>تم إكمال الطلب  من خلال مورد آخر.</p>
                        <p>نشكرك على اهتمامك ونشجعك على متابعة الفرص الجديدة على منصتنا.</p>
                        <p>مع تحياتنا،<br><strong>فريق SuppliFy</strong></p>
                      </div>
                    </body>
                    </html>";
            }
            else if (order.Deadline < DateTime.UtcNow)
            {
                Body = $@"
                        <html>
                        <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                          <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px;'>
                            <h2 style='color:#ffc107;'> انتهى الموعد النهائي</h2>
                            <p>انتهى الموعد النهائي لهذا الطلب بتاريخ <strong>{order.Deadline:dd MMM yyyy}</strong>.</p>
                            <p>نشكرك على اهتمامك ونشجعك على متابعة الفرص الجديدة على منصتنا.</p>
                            <p>مع تحياتنا،<br><strong>فريق SuppliFy</strong></p>
                          </div>
                        </body>
                        </html>";
                checkToUpdateOrderStatus = true;
            }
            else
            {
                if (user.Supplier.SupplierSubscriptionPlan.NumberOfAcceptOrder <= 0)
                    return Content(@"
                                <html lang=""ar"" dir=""rtl"">
                                <head>
                                  <meta charset=""UTF-8"">
                                  <title>رسالة</title>
                                </head>
                                    <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                                      <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px; text-align:center;'>
                                        <h2 style='color:#fd7e14;'> الحد الأقصى للخطة</h2>
                                        <p>لقد وصلت إلى الحد الأقصى للطلبات المسموح بها في خطتك الحالية.<br>قم بالترقية للحصول على المزيد!</p>
                                      </div>
                                    </body>
                                    </html>", "text/html");
                // Decrease the number of AcceptOrders left for the supplier
                user.Supplier.SupplierSubscriptionPlan.NumberOfAcceptOrder--;
                order.NumSuppliersDesired--;
                _unitOfWork.SupplierSubscriptionPlans.Update(user.Supplier.SupplierSubscriptionPlan);
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveAsync();
                Body = $@"
                        <html>
                        <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                          <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px;'>
                            <h2 style='color:#007bff;'> بيانات التواصل</h2>
                            <p>إليك بيانات التواصل الخاصة بالطلب:</p>
                            <ul style='line-height:1.8;'>
                              <li><strong>اسم العميل:</strong> {order.User.Name}</li>
                              <li><strong>البريد الإلكتروني:</strong> {order.User.Email}</li>
                              <li><strong>جوال الشركة:</strong> {order.User.Phone}</li>
                              <li><strong>الشخص المسؤول:</strong> {order.ContactPersonName}</li>
                              <li><strong>جوال المسؤول:</strong> {order.ContactPersonNumber}</li>
                            </ul>
                            <p>مع تحياتنا،<br><strong>فريق SuppliFy</strong></p>
                          </div>
                        </body>
                        </html>";
            }
            if (checkToUpdateOrderStatus && order.OrderStatus == OrderStatus.Active)
            {
                order.OrderStatus = OrderStatus.InProgress;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveAsync();
            }
            contactEmail.Body = Body;
            _emailService.SendEmail(contactEmail);
            return Content(@"
                        <html lang=""ar"" dir=""rtl"">
                        <head>
                          <meta charset=""UTF-8"">
                          <title>رسالة</title>
                        </head>
                        <body style='font-family: Arial,sans-serif; background:#f9f9f9; padding:20px; direction:rtl; text-align:right;'>
                          <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:8px; padding:20px; text-align:center;'>
                            <h2 style='color:#28a745;'> تم إرسال البيانات</h2>
                            <p>تم إرسال بيانات التواصل إلى بريدك الإلكتروني.<br>هذا الرابط تم استخدامه بالفعل ولا يمكن استخدامه مرة أخرى.</p>
                          </div>
                        </body>
                        </html>", "text/html");
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a list of all uncompleted and completed orders for a specific client.
        /// It is accessible only to authenticated users with the "Clients" role.
        ///
        /// **Flow & Logic:**
        /// 1. **User Validation:** The method first validates the user's claims to extract the `userId`.
        /// 2. **Data Retrieval:** It calls a repository method to get all uncompleted or completed orders
        ///    associated with that client's `userId`.
        /// 3. **Data Mapping:** The retrieved order data is then mapped to a `ShowOrderForClientDTO` list.
        ///    - **Deal Status Logic:** The `DealStatus` is determined based on the order's state.
        ///      - If there is no associated `Deal`, it checks if the `OrderStatus` is `Canceled`.
        ///      - If so, the `DealStatus` is set to `AdminConfirmed`.
        ///      - Otherwise, it's set to `Pending`.
        ///      - If a `Deal` exists, its `Status` is used directly.
        /// 4. **Response:** The function returns a general success response containing the list
        ///    of mapped orders for the client.
        /// </summary>
        [Authorize(Roles = "Clients")]
        [HttpGet("client/deals")]
        public async Task<IActionResult> GetUnCompletedOrCompletedOrdersForClient()
        {
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("there is problems in Claims"));
            List<Order> orders = await _unitOfWork.Orders.GetAllUnCompletedOrCompletedOrdersForUser(userId);
            List<ShowOrderForClientDTO> showOrderForClientDTOs = orders.Select(o => new ShowOrderForClientDTO()
            {
                OrderId = o.Id,
                CategoryId = o.CategoryId,
                ContactPersonName = o.ContactPersonName,
                ContactPersonPhone = o.ContactPersonNumber,
                Deadline = o.Deadline,
                
                NumSuppliersDesired = o.NumSuppliersDesired,
                
                RequiredLocation = o.RequiredLocation,
                DealStatus = o.Deal is null ?o.OrderStatus == OrderStatus.Canceled ? DealStatus.AdminConfirmed : DealStatus.Pending : o.Deal.Status,
                OrderStatus = o.OrderStatus,
                Items= o.Items.Select(i => new OrderItemToShowDto
                {
                    ItemId = i.Id,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Notes = i.Notes
                }).ToList()

            }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(showOrderForClientDTOs));
        }


        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a list of all unreviewed or reviewed deals for a specific supplier.
        /// It is accessible only to authenticated users with the "Suppliers" role.
        ///
        /// **Flow & Logic:**
        /// 1. **User Validation:** The method first validates the user's claims to extract the `userId`
        ///    and then finds the corresponding `Supplier` entity.
        /// 2. **Data Retrieval:** It calls a repository method to get all unreviewed or reviewed `Deal`
        ///    entities associated with that supplier.
        /// 3. **Data Mapping:** The retrieved deal data is then mapped to a `ShowDealForSupplierDTO` list,
        ///    which includes relevant information from the related `Order` and `Client` entities.
        /// 4. **Response:** The function returns a general success response containing the list
        ///    of mapped deals for the supplier.
        /// </summary>
        [Authorize(Roles ="Suppliers")]
        [HttpGet("supplier/deals")]
        public async Task<IActionResult> GetUnCompletedOrCompletedOrdersForSupplier()
        {
            if (!int.TryParse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value, out int userId))
                return BadRequest(ResponseFactory.CreateMessageResponse("there is problems in Claims"));
            Supplier supplier = await _unitOfWork.Suppliers.FindAsync(s=> s.UserId == userId);
            if (supplier == null)
                return BadRequest(ResponseFactory.CreateMessageResponse("Invalid User Id"));
            List<Deal> deals= await _unitOfWork.Deals.GetAllUnreviewedOrReviewedDealsForSupplier(supplier.Id);
            List<ShowDealForSupplierDTO> showDealForSupplierDTOs = deals.Select(d => new ShowDealForSupplierDTO()
            {
                DealId =d.Id,
                CompanyEmail = d.Client.Email,
                CompanyName = d.Client.Name,
                CompanyPhone = d.Client.Phone,
                ContactPersonName = d.Order.ContactPersonName,
                ContactPersonPhone = d.Order.ContactPersonNumber,

                // With the following, which selects the items from the most recent DealDetailsVerification:
                Items = d.DealDetailsVerifications
                .SelectMany(v => v.Items)
                .Select(i => new DealItemToShowDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                DealStatus = d.Status,
              OrderStatus = d.Order.OrderStatus
            }).ToList();
            return Ok(ResponseFactory.CreateGeneralResponse(showDealForSupplierDTOs));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows a client to cancel an existing order.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Clients" role.
        /// 2. **Input Validation:** It takes an `orderId` from the route and first checks if the order exists in the database.
        /// 3. **Order Status Update:** If the order is found, its `OrderStatus` is changed to `Canceled`.
        /// 4. **Database Save:** The updated order is saved to the database. The method checks if the save operation was successful.
        /// 5. **Response:** It returns an `Ok` response with a success message if the order is successfully canceled.
        ///    If the order is not found or the database update fails, it returns a `BadRequest` with an appropriate error message.
        /// </summary>
        [Authorize(Roles ="Clients")]
        [HttpPatch("client/cancel-order/{orderId:int}")]
        public async Task<IActionResult> GetUnCompletedOrdersForAdmin([FromRoute] int orderId)
        {
            Order order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("الأوردر غير موجود"));
            order.OrderStatus = OrderStatus.Canceled;
            _unitOfWork.Orders.Update(order);
            if (await _unitOfWork.SaveAsync() != 1)
                return BadRequest(ResponseFactory.CreateMessageResponse("فشل إلغاء الأوردر حاول مرة أخرى"));
            return Ok(ResponseFactory.CreateMessageResponse("تم إلغاء الأوردر"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint retrieves a paginated list of all pending supplier requests to accept orders for admin review.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Filtering Criteria:** It defines a filter to only retrieve requests with a status of "Pending".
        /// 3. **Pagination & Sorting:** It counts the total number of matching items and then builds a query
        ///    to fetch a paginated and sorted subset of the requests. The results are sorted in descending order
        ///    based on their creation date.
        /// 4. **Data Mapping:** The retrieved requests are mapped to `RequestToAddMoreDto` objects,
        ///    which include the request ID, supplier's name, email, phone, and the requested amount.
        /// 5. **Response:** The function returns a paginated success response containing the list
        ///    of mapped requests and metadata about the pagination (current page, page size, total pages, total items).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/accept-order-request")]
        public async Task<IActionResult> GetAdvertisementRequests(int page = 1, int pageSize = 10)
        {
            Expression<Func<SupplierAcceptOrderRequest, bool>> criteria = r => r.Status == Enum.RequestStatus.Pending;
            int totalItems = await _unitOfWork.SupplierAcceptOrderRequests.CountAsync(criteria);
            // 4. Build the query
            var supplierAcceptOrderRequests = await _unitOfWork.SupplierAcceptOrderRequests.FindWithFiltersAsync(
                9,
                criteria: criteria,
                sortColumn: nameof(SupplierAdvertisementRequest.CreatedAt),
                sortColumnDirection: Enum.OrderBy.Desc.ToString(),
                skip: (page - 1) * pageSize,
                take: pageSize
            );
            var result = supplierAcceptOrderRequests.Select(r => new RequestToAddMoreDto
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
        /// This endpoint allows an administrator to either accept or reject a pending supplier request to accept an order.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is accessible only to users with the "Admin" role.
        /// 2. **Input Validation:** It validates the provided `requestId` and `operationType` to ensure they are valid.
        /// 3. **Request Retrieval:** It finds the specific `SupplierAcceptOrderRequest` by `requestId` and confirms it has a "Pending" status. If not found, it returns an error.
        /// 4. **Status Update:**
        ///    - If the `operationType` is "Accept," the request's status is updated to `Approved`.
        ///    - If the `operationType` is "Cancel," the request's status is updated to `Rejected`.
        /// 5. **Supplier Plan Update:** For an "Accept" operation, the supplier's `NumberOfSpecialProduct` is increased by the `RequestedAmount` from the request.
        /// 6. **Database Save:** The changes are saved to the database.
        /// 7. **Response:** The method returns an appropriate success or failure message in Arabic based on the outcome of the operation.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/accept-cancel-accepted-order-request")]
        public async Task<IActionResult> AdminAcceptOrCancelAcceptedOrderRequest(OperationOnRequestType operationType, Guid requestId)
        {
            if (requestId == Guid.Empty)
                return BadRequest(ResponseFactory.CreateMessageResponse("Id حقل مطلوب"));
            if (!System.Enum.IsDefined(typeof(OperationOnRequestType), operationType))
                return BadRequest(ResponseFactory.CreateMessageResponse("عملية غير صالحة"));


            SupplierAcceptOrderRequest supplierAcceptOrderRequest = await _unitOfWork.SupplierAcceptOrderRequests.FindAsync(r => r.Id == requestId && r.Status == RequestStatus.Pending,
                ["Supplier", "Supplier.SupplierSubscriptionPlan"]);
            bool check;
            if (supplierAcceptOrderRequest is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("طلب غير موجود"));
            supplierAcceptOrderRequest.ProcessedAt = DateTime.UtcNow;
            if (operationType == OperationOnRequestType.Accept)
            {
                supplierAcceptOrderRequest.Status = RequestStatus.Approved;
                supplierAcceptOrderRequest.Supplier.SupplierSubscriptionPlan.NumberOfSpecialProduct += supplierAcceptOrderRequest.RequestedAmount;
                check = true;
            }
            else
            {
                supplierAcceptOrderRequest.Status = RequestStatus.Rejected;
                check = false;
            }
            _unitOfWork.SupplierAcceptOrderRequests.Update(supplierAcceptOrderRequest);
            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse(check ? "فشل قبول الطلب" : "فشل رفض الطلب"));
            return Ok(ResponseFactory.CreateMessageResponse(check ? "تم قبول الطلب" : "فشل رفض الطلب"));
        }

        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an administrator to retrieve a list of deals that are pending confirmation.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Data Retrieval:** It calls a specific repository method to get all deals that require admin confirmation, including their associated client and supplier information.
        /// 3. **Data Mapping:** The retrieved deals are mapped to `ShowDealsForAdminDTO` objects.
        ///    - It populates the client's deal details from the most recent verification entry.
        ///    - It populates the supplier's deal details from the first verification entry.
        ///    - The DTO includes key deal information such as company names, contact details, prices, and dates.
        /// 4. **Response:** The function returns a successful response containing the list of mapped deals.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/deals-confirm")]
        public async Task<IActionResult> GetDealsToConfirmByAdmin()
        {
            var deals = await _unitOfWork.Orders.GetAllDealsToConfirmByAdminWithClientAndSupplier();

            var showDealsForAdminDTOs = deals.Select(d => new ShowDealsForAdminDTO()
            {
                DealId = d.Id,

                
                ClientDealDetails = d.DealDetailsVerifications
                    .Where(v => v.SubmittedById == d.Client.Id)
                    .Select(v => new ShowDealDetailsForAdminDTO
                    {
                        DealDetailsId = v.Id,
                        CompanyEmail = d.Client.Email,
                        CompanyName = d.Client.Name,
                        CompanyPhone = d.Client.Phone,
                        DateOfDelivered = v.DateOfDelivered,
                        DealDoneAt = v.DealDoneAt,
                        
                        Items = v.Items.Select(i => new DealItemToShowDto
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    })
                    .FirstOrDefault(), 

                
                SupplierDealDetails = d.DealDetailsVerifications
                    .Where(v => v.SubmittedById == d.Supplier.UserId)
                    .Select(v => new ShowDealDetailsForAdminDTO
                    {
                        DealDetailsId = v.Id,
                        CompanyEmail = d.Supplier.User.Email,
                        CompanyName = d.Supplier.User.Name,
                        CompanyPhone = d.Supplier.User.Phone,
                        DateOfDelivered = v.DateOfDelivered,
                        DealDoneAt = v.DealDoneAt,
                        
                        Items = v.Items.Select(i => new DealItemToShowDto
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    })
                    .FirstOrDefault() // واحدة فقط
            }).ToList();

            return Ok(ResponseFactory.CreateGeneralResponse(showDealsForAdminDTOs));
        }


        /// <summary>
        /// **Function Summary:**
        /// This endpoint allows an administrator to either accept or reject a deal.
        ///
        /// **Flow & Logic:**
        /// 1. **Authorization:** The endpoint is restricted to users with the "Admin" role.
        /// 2. **Input Validation:** It validates the provided `dealId` and the `operationType` to ensure they are valid.
        /// 3. **Deal Retrieval:** The code fetches the specific `Deal` from the database using its `dealId`, including the related `Order` information. It returns an error if the deal is not found.
        /// 4. **Status Update:**
        ///    - If the `operationType` is `Accept`, the deal's status is updated to `AdminConfirmed`, and the related order's status is set to `Completed`.
        ///    - If the `operationType` is anything else, the deal's status is set to `AdminRefused`, and the order's status is set to `Canceled`.
        /// 5. **Database Save:** The changes to the deal and order are saved to the database.
        /// 6. **Response:** The method returns an appropriate success or failure message in Arabic based on the outcome of the database save operation.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/accept-cancel-deal-request")]
        public async Task<IActionResult> AdminAcceptOrCancelDealRequest(OperationOnRequestType operationType, int dealId)
        {
            if (dealId <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("مطلوب إدخال رقم الديل"));
            if (!System.Enum.IsDefined(typeof(OperationOnRequestType), operationType))
                return BadRequest(ResponseFactory.CreateMessageResponse("تم إدخال نوع عملية غير صالح"));

            var deal = await _unitOfWork.Deals.FindAsync(d => d.Id == dealId,["Order"]);

            if (deal is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("الديل غير موجود"));

            bool isAccepted;
            if (operationType == OperationOnRequestType.Accept)
            {
                deal.Status = DealStatus.AdminConfirmed;
                deal.Order.OrderStatus = OrderStatus.Completed;
                isAccepted = true;
            }
            else
            {
                deal.Status = DealStatus.AdminRefused;
                deal.Order.OrderStatus = OrderStatus.Canceled;
                isAccepted = false;
            }
            _unitOfWork.Deals.Update(deal);

            if (await _unitOfWork.SaveAsync() == 0)
                return BadRequest(ResponseFactory.CreateMessageResponse(isAccepted ? "فشل قبول الديل" : "فشل رفض الديل"));

            return Ok(ResponseFactory.CreateMessageResponse(isAccepted ? "تم قبول الديل بنجاح" : "تم رفض الديل بنجاح"));
        }

    }
}
