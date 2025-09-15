using DAL.Data;
using Entities;
using Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        protected AppDBContext _context;
        private IDbContextTransaction _currentTransaction;
        public UnitOfWork(AppDBContext context)
        {
            _context = context;
            Users = new Repository<User>(_context);
            Suppliers = new Repository<Supplier>(_context);
            Categories = new Repository<Category>(_context);
            Roles = new Repository<Role>(_context);
            JopSeekers = new Repository<JopSeeker>(_context);
            JopSeekerCategoryApplies = new Repository<JopSeekerCategoryApply>(_context);
            SupplierSubscriptionPlans = new Repository<SupplierSubscriptionPlan>(_context);
            JobPosts = new Repository<JobPost>(_context);
            Orders = new Repository<Order>(_context);
            SubscriptionPlans = new Repository<SubscriptionPlan>(_context);
            UserRoles = new Repository<UserRole>(_context);
            UserTokens = new Repository<UserToken>(_context);
            SupplierCategories = new Repository<SupplierCategory>(_context);
            Products = new Repository<Product>(_context);
            SpecialProducts = new Repository<SpecialProduct>(_context);
            //PasswordResetTokens = new Repository<PasswordResetToken>(_context);
            Reviews = new Repository<Review>(_context);
            Deals = new Repository<Deal>(_context);
            DealDetailsVerifications = new Repository<DealDetailsVerification>(_context);
            SupplierAdvertisements = new Repository<SupplierAdvertisement>(_context);
            SupplierSubscriptionPlanArchives = new Repository<SupplierSubscriptionPlanArchive>(_context);
            UnconfirmedSupplierSubscriptionPlans = new Repository<UnconfirmedSupplierSubscriptionPlan>(_context);
            SupplierProductRequests = new Repository<SupplierProductRequest>(_context);
            SupplierAdvertisementRequests = new Repository<SupplierAdvertisementRequest>(_context);
            SupplierAcceptOrderRequests = new Repository<SupplierAcceptOrderRequest>(_context);
            UserRequestCategories = new Repository<UserRequestCategory>(_context);
            OrderItems = new Repository<OrderItem>(_context);
            DealItems = new Repository<DealItem>(_context);
            Blogs = new Repository<Blog>(_context);
        }
        public IRepository<SpecialProduct> SpecialProducts { get; }
        public IRepository<SupplierProductRequest> SupplierProductRequests { get; }
        public IRepository<SupplierAdvertisementRequest> SupplierAdvertisementRequests { get; }
        public IRepository<SupplierAcceptOrderRequest> SupplierAcceptOrderRequests { get; }
        public IRepository<SupplierSubscriptionPlanArchive> SupplierSubscriptionPlanArchives { get; }
        //public IRepository<PasswordResetToken> PasswordResetTokens { get; }
        public IRepository<Review> Reviews { get; }
        public IRepository<Deal> Deals { get; }
        public IRepository<DealDetailsVerification> DealDetailsVerifications { get; }
        public IRepository<User> Users { get; }
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
        public IRepository<UnconfirmedSupplierSubscriptionPlan> UnconfirmedSupplierSubscriptionPlans { get; }
        public IRepository<UserRequestCategory> UserRequestCategories { get; }
        public IRepository<OrderItem> OrderItems { get; }
        public IRepository<DealItem> DealItems { get; }
        public IRepository<Blog> Blogs { get; }
        public void Dispose()
        {
            _context.Dispose();
            _currentTransaction?.Dispose();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            // Check if a transaction is already in progress to avoid nesting issues.
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            // Use the DbContext to begin a new transaction.
            _currentTransaction = await _context.Database.BeginTransactionAsync();

            return _currentTransaction;
        }

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            try
            {
                if (_currentTransaction == null)
                {
                    throw new InvalidOperationException("No transaction to commit.");
                }
                // Commit the transaction, making all changes permanent.
                await _currentTransaction.CommitAsync();
            }
            catch
            {
                // If the commit fails, an exception is thrown.
                // In a real-world scenario, you might log the error here.
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                // Always dispose of the transaction object.
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current transaction, undoing all changes.
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_currentTransaction == null) return;
                // Roll back the transaction to undo all pending changes.
                await _currentTransaction.RollbackAsync();
            }
            finally
            {
                // Always dispose of the transaction object.
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }



        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public int Save()
        {
            return _context.SaveChanges();
        }
    }

}
