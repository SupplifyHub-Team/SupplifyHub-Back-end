using Entities;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
namespace DAL.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        // DbSet properties for your entities
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<SupplierSubscriptionPlan> SuppliersSubscriptionPlans { get; set; }
        public DbSet<JopSeeker> JopSeekers { get; set; }
        public DbSet<JopSeekerCategoryApply> JopSeekerCategoryApplies { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<SupplierCategory> SupplierCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<SpecialProduct> SpecialProducts { get; set;}
        //public DbSet<PasswordResetToken> PasswordResetTokens { get; set;}
        public DbSet<Review> Reviews { get; set;}
        public DbSet<Deal> Deals { get; set;}
        public DbSet<DealDetailsVerification> DealDetailsVerifications { get; set; }
        public DbSet<SupplierAdvertisement> SupplierAdvertisements { get; set; }
        public DbSet<SupplierSubscriptionPlanArchive> SupplierSubscriptionPlanArchives { get; set; }
        public DbSet<UnconfirmedSupplierSubscriptionPlan> UnconfirmedSupplierSubscriptionPlans { get; set; }
        public DbSet<SupplierProductRequest> SupplierProductRequests { get; set; }
        public DbSet<SupplierAdvertisementRequest> SupplierAdvertisementRequests { get; set; }
        public DbSet<SupplierAcceptOrderRequest> SupplierAcceptOrderRequests { get; set; }
        public DbSet<UserRequestCategory> UserRequestCategories { get; set;}
        public DbSet<DealItem> DealItems { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Apply all configurations
            builder.ApplyConfigurationsFromAssembly(typeof(AppDBContext).Assembly);
        }
    }
}
