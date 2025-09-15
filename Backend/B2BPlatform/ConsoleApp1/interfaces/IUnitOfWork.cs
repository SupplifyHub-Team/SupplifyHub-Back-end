
using Entities;
using Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Models.Entities;

//using Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        public IRepository<User> Users { get; }
        public IRepository<SpecialProduct> SpecialProducts { get; }
        //public IRepository<PasswordResetToken>  PasswordResetTokens { get; }
        public IRepository<Review> Reviews { get; }
        public IRepository<Deal> Deals { get; }
        public IRepository<DealDetailsVerification> DealDetailsVerifications { get; }
        public IRepository<Product> Products { get; }
        public IRepository<Supplier> Suppliers { get; }
        public IRepository<Category> Categories { get; }

        public IRepository<Role> Roles { get; } 
        public IRepository<JopSeeker> JopSeekers { get; } 
        public IRepository<JopSeekerCategoryApply> JopSeekerCategoryApplies { get; } 
        public IRepository<SupplierSubscriptionPlan> SupplierSubscriptionPlans { get; } 
        public IRepository<JobPost> JobPosts { get; } 
        public IRepository<Order> Orders { get; } 
        public IRepository<SubscriptionPlan> SubscriptionPlans { get; } 
        public IRepository<UserRole> UserRoles { get; } 
        public IRepository<UserToken> UserTokens { get; } 
        public IRepository<SupplierCategory> SupplierCategories { get; }
        public IRepository<SupplierAdvertisement> SupplierAdvertisements { get; }
        public IRepository<SupplierSubscriptionPlanArchive> SupplierSubscriptionPlanArchives { get; }
        public IRepository<UnconfirmedSupplierSubscriptionPlan> UnconfirmedSupplierSubscriptionPlans { get; }
        public IRepository<SupplierAcceptOrderRequest> SupplierAcceptOrderRequests { get; }
        public IRepository<SupplierAdvertisementRequest> SupplierAdvertisementRequests { get; }
        public IRepository<SupplierProductRequest> SupplierProductRequests { get; }
        public IRepository<UserRequestCategory> UserRequestCategories { get; }
        public IRepository<DealItem> DealItems { get; }
        public IRepository<OrderItem> OrderItems { get; }
        public IRepository<Blog> Blogs { get; }
        int Save();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> SaveAsync();
    }
}
