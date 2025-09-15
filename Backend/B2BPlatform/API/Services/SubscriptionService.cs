using API.DTO;
using Entities;
using Enum;
using Interfaces;
using YourNamespace;



namespace API.Services
{
    /// <summary>
    /// Provides subscription management services for suppliers.  
    /// 
    /// This service includes the following main functionalities:  
    /// 1. Sending reminder emails to suppliers before their subscription expires.  
    /// 2. Handling expired subscriptions by:  
    ///    - Downgrading the supplier to the free plan.  
    ///    - Removing all supplier products and advertisements.  
    ///    - Deleting related images from Cloudinary.  
    /// 
    /// Used by background jobs or scheduled tasks to ensure subscription plans  
    /// are correctly enforced and suppliers are notified in time.  
    /// </summary
    public class SubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ICloudinaryService _cloudinaryService;

        public SubscriptionService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IEmailService emailService, IConfiguration configuration)
        {
            this._unitOfWork = unitOfWork;
            this._emailService = emailService;
            this._configuration = configuration;
            this._cloudinaryService = cloudinaryService;
        }
        public async Task SendEmailToSupplierToUpdateSubscriptionAsync()
        {
            var users = await _unitOfWork.Users.FindAllAsync(u => u.Supplier != null 
            && u.Supplier.SupplierSubscriptionPlan.PlanId != 4 
            && u.Supplier.SupplierSubscriptionPlan.EndDate <= DateTime.UtcNow.AddDays(5)
            , ["Supplier", "Supplier.SupplierSubscriptionPlan"]);
            EmailDto emailDto= new EmailDto();
            foreach (var user in users)
            {
                var subscriptionEndDate = user.Supplier.SupplierSubscriptionPlan.EndDate.ToShortDateString();
                var billingPageUrl = "https://b2b-platform-nu.vercel.app/en";
                var emailBody = $@"
<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; direction: rtl; text-align: right; }}
        .container {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1); }}
        .header {{ background-color: #007BFF; color: #ffffff; padding: 20px; text-align: center; }}
        .content {{ padding: 20px 30px; line-height: 1.8; color: #333333; }}
        .button {{ display: inline-block; background-color: #007BFF; color: #ffffff; padding: 12px 24px; margin: 20px 0; border-radius: 5px; text-decoration: none; font-weight: bold; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #999999; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>تنبيه بانتهاء الاشتراك</h2>
        </div>
        <div class='content'>
            <p>مرحباً {user.Name},</p>
            <p>نود إبلاغك بأن اشتراكك سينتهي في تاريخ <strong>{subscriptionEndDate}</strong>.</p>
            <p>لضمان استمرار عرض منتجاتك وإعلاناتك دون انقطاع أو حذف، يرجى تجديد الاشتراك قبل هذا التاريخ.</p>
            <p>يمكنك تجديد اشتراكك عن طريق تسجيل الدخول إلى حسابك وزيارة قسم <strong>الفواتير</strong> أو <strong>الاشتراكات</strong>.</p>
            <a href='{billingPageUrl}' class='button'>جدد اشتراكك الآن</a>
            <p>شكراً لكونك أحد عملائنا المميزين!</p>
            <p>مع خالص التحية،<br>
            فريق SuppliFy</p>
        </div>
        <div class='footer'>
            <p>تم إرسال هذا البريد إلى {user.Email} لأن لديك حساباً على SuppliFy.</p>
        </div>
    </div>
</body>
</html>
";

                emailDto.To = user.Email;
                emailDto.Subject = "ستنتهي صلاحية اشتراكك قريباً";
                emailDto.Body = emailBody;
                _emailService.SendEmail(emailDto);
            }
        }
        public async Task DeleteProductAndAdsForExpiredPlansAsync()
        {
            // Get all suppliers whose plan has expired.
            var expiredSuppliers = await _unitOfWork.Suppliers
                .FindAllAsync(s => s.SupplierSubscriptionPlan.EndDate <= DateTime.UtcNow, ["SupplierSubscriptionPlan"]);
            SubscriptionPlan subscriptionPlan = _unitOfWork.SubscriptionPlans.GetById(4);
            foreach (var supplier in expiredSuppliers)
            {
                // Set the plan to 'Free' or a 'Grace' period plan.
                supplier.SupplierSubscriptionPlan.CompetitorAndMarketAnalysis = false;
                supplier.SupplierSubscriptionPlan.ProductVisitsAndPerformanceAnalysis = false;
                supplier.SupplierSubscriptionPlan.CreatedAt = DateTime.UtcNow;
                supplier.SupplierSubscriptionPlan.DirectTechnicalSupport = false;
                supplier.SupplierSubscriptionPlan.EarlyAccessToOrder = false;
                supplier.SupplierSubscriptionPlan.EndDate = DateTime.UtcNow.AddMonths(subscriptionPlan.Duration);
                supplier.SupplierSubscriptionPlan.NumberOfAcceptOrder = 30;
                supplier.SupplierSubscriptionPlan.NumberOfAdvertisement = 0;
                supplier.SupplierSubscriptionPlan.NumberOfProduct = 0;
                supplier.SupplierSubscriptionPlan.NumberOfSpecialProduct = 0;
                supplier.SupplierSubscriptionPlan.PaymentStatus = PaymentStatus.Completed;
                supplier.SupplierSubscriptionPlan.PlanName = subscriptionPlan.Name;
                supplier.SupplierSubscriptionPlan.ShowHigherInSearch = false;
                supplier.SupplierSubscriptionPlan.StartDate = DateTime.UtcNow;
                supplier.SupplierSubscriptionPlan.PlanId = subscriptionPlan.Id;
                // Deactivate all their products.
                var supplierProducts = await _unitOfWork.Products
                    .FindAllAsync(p => p.SupplierId == supplier.Id);
                var supplierAdvertisements= await _unitOfWork.SupplierAdvertisements
                .FindAllAsync(p => p.SupplierId == supplier.Id);
                foreach (var product in supplierProducts)
                {
                    await _cloudinaryService.DeleteAsync(product.ImagePublicId);
                }
                foreach (var advertisement in supplierAdvertisements)
                {
                    await _cloudinaryService.DeleteAsync(advertisement.ImagePublicId);
                }
                _unitOfWork.SupplierAdvertisements.DeleteRange(supplierAdvertisements);
                _unitOfWork.Products.DeleteRange(supplierProducts);
                await _unitOfWork.SaveAsync();
            }
        }
    }


}