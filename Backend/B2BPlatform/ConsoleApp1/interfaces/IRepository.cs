using Entities;
using Enum;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;


namespace Interfaces 
{
    public interface IRepository<T> where T : class
    {
        Task<List<Deal>> GetAllUnreviewedOrReviewedDealsForSupplier(int supplierId);

        Task<List<Deal>> GetAllDealsToConfirmByAdminWithClientAndSupplier();
        Task<List<Order>> GetAllUnCompletedOrders();
        Task<List<Order>> GetAllUnCompletedOrCompletedOrdersForUser(int userId);
        Task<List<UserToken>> GetAllUserTokenThatNotValidateAsync();
        Task<List<UserToken>> GetALLUserTokenThatIsValidByUserIdAsync(int userId);
        //Task<List<PasswordResetToken>> GetAllResetPasswordTokenThatNotValidateAsync();
        //Task < List<PasswordResetToken>> GetALLPasswordResetTokenThatIsValidAsync();
        Task<T> GetByUserIdAsync(int UserId);
        bool CheckExistByEmail(string Email);
        bool CheckExistByUserName(string name);
        bool CheckExistByCompanyName(string CompanyName);
        T GetById(int id);
        //Task<UserToken?> GetByIdLastToken(int id);
        Task<Role?> GetRoleByRoleNameAsync(RoleName roleName);
        Task<string> GenerateToken(User user, int Duration, IConfiguration configuration);
        Task<T> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string Email);
        Task<User?> GetByEmailIncludeRolesAsync(string Email);
        Task<T> GetByCompositeAsync(params object[] keys);
        Task<T> GetByNameAsync(string name);
        Task<T> GetByColumnAsync(string columnName, string value);
        IEnumerable<T> GetAll();
        Task<IEnumerable<T>> GetAllAsync();
         Task<IQueryable<User>> GetUsersWithCategoryAndRole();
        Task<List<User>> GetUsersWithUserRoles();
        public List<string> GetDistinct(Expression<Func<T, string>> col);
        T Find(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes = null);
        IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, string[] includes = null);
        IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int take, int skip);
        IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int? take, int? skip,
            Expression<Func<T, object>> orderBy = null, bool IsDesc = false);
        Task<IEnumerable<T>> FindAllAsyncWithoutCraiteria(string[] includes = null);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, int skip, int take);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, int? skip, int? take,
            Expression<Func<T, object>> orderBy = null, bool IsDesc = false);
        
        IEnumerable<T> FindWithFilters(
        Expression<Func<T, bool>> criteria = null,
        string sortColumn = null,
        string sortColumnDirection = null,
        int? skip = null,
        int? take = null);

        Task<IEnumerable<T>> FindWithFiltersAsync(
        int queryId,
        Expression<Func<T, bool>> criteria = null,
        string sortColumn = null,
        string sortColumnDirection = null,
        int? skip = null,
        int? take = null);

        T Add(T entity);
        Task<T> AddAsync(T entity);
        IEnumerable<T> AddRange(IEnumerable<T> entities);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        T Update(T entity);
        bool UpdateRange(IEnumerable<T> entities);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);
        int Count();
        int Count(Expression<Func<T, bool>> criteria);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> criteria);

        Task<Int64> MaxAsync(Expression<Func<T, object>> column);

        Task<Int64> MaxAsync(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> column);

        Int64 Max(Expression<Func<T, object>> column);

        Int64 Max(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> column);
        public bool IsExist(Expression<Func<T, bool>> criteria);
        T Last(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> orderBy);
        Task<T?> GetBySSNAsync(string SSN);

    }
}
