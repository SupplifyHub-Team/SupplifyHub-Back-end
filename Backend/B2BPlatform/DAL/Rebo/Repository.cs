using DAL.Data;
using Entities;
using Enum;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Models.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace DAL
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected AppDBContext _context;

        public Repository(AppDBContext context)
        {
            _context = context;
        }
        public async Task<List<Deal>> GetAllDealsToConfirmByAdminWithClientAndSupplier() =>
            await _context.Deals.Where(x => x.Status == DealStatus.SupplierConfirmed)
            .Include(x => x.Supplier).ThenInclude(x => x.User).Include(o => o.Client).Include(d=>d.DealDetailsVerifications).ThenInclude(k=>k.Items).ToListAsync();
        public async Task<List<Order>> GetAllUnCompletedOrCompletedOrdersForUser(int userId)
        => await _context.Orders.Where(o => o.UserId == userId)
            .Include(d => d.Deal).Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToListAsync();
        public async Task<List<Deal>> GetAllUnreviewedOrReviewedDealsForSupplier(int supplierId)
        => await _context.Deals.Where(d => d.SupplierId == supplierId).OrderByDescending(d=>d.Order.CreatedAt)
            .Include(d=>d.Client).Include(d=>d.Order).ThenInclude(i=>i.Items)
            .Include(v=>v.DealDetailsVerifications).ThenInclude(l=>l.Items).ToListAsync();

        public async Task<List<Order>> GetAllUnCompletedOrders()
        => await _context.Orders.Where(o => o.OrderStatus == OrderStatus.InProgress).ToListAsync();

        //public async Task<List<PasswordResetToken>> GetALLPasswordResetTokenThatIsValidAsync() =>
        //await _context.PasswordResetTokens
        //.Where(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
        //.Include(t => t.User) // Include user data for later use.
        //.ToListAsync();
        public async Task<List<UserToken>> GetALLUserTokenThatIsValidByUserIdAsync(int userId) =>
        await _context.UserTokens
        .Where(t => !t.IsRevoked && t.UserId == userId && t.ExpiresAt > DateTime.UtcNow)
        .ToListAsync();
        //public async Task<List<PasswordResetToken>> GetAllResetPasswordTokenThatNotValidateAsync() =>
        // await _context.PasswordResetTokens.Where(t => t.ExpiresAt < DateTime.UtcNow || t.IsUsed)
        //    .ToListAsync();
        public async Task<List<UserToken>> GetAllUserTokenThatNotValidateAsync() =>
        await _context.UserTokens.Where(t => t.ExpiresAt < DateTime.UtcNow || t.IsRevoked)
        .ToListAsync();


        public Task<Role?> GetRoleByRoleNameAsync(RoleName roleName)
        {
            return _context.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
        }
        //public Task<UserToken?> GetByIdLastToken(int id)
        //{
        //    return _context.UserTokens
        //                 .Where(x => x.UserId == id)
        //                 .OrderByDescending(x => x.Id) // Order by the indexed column
        //                 .FirstOrDefaultAsync(); // Get the first (latest) after ordering;
        //}
        public async Task<User?> GetByEmailAsync(string Email)
        {
            return await _context.Users.FirstOrDefaultAsync(e => e.Email == Email);
        }
        public async Task<User?> GetByEmailIncludeRolesAsync(string Email)
        {
            return await _context.Users.Include(u=> u.UserRoles).ThenInclude(ur=>ur.Role).Include(u=>u.Supplier).FirstOrDefaultAsync(e => e.Email == Email);
        }

        public async Task<string> GenerateToken(User user, int Duration, IConfiguration configuration)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ];
            List<UserRole> UserRoles = await _context.UserRoles.Include(x => x.Role).Where(x => x.UserId == user.Id).ToListAsync();
            foreach (var UserRole in UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, UserRole.Role.Name.ToString()));
            }
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]!));
            SigningCredentials signing = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken DescriptionToken = new JwtSecurityToken
                (
                    issuer: configuration["JWT:Issuer"],
                    audience: configuration["JWT:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Duration),
                    signingCredentials: signing
                );
            string token = new JwtSecurityTokenHandler().WriteToken(DescriptionToken);

            return token;
        }

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }
        public async Task<T> GetByCompositeAsync(params object[] keys)
        {
            return await _context.Set<T>().FindAsync(keys);
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<T> GetByNameAsync(string name)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(e => EF.Property<string>(e, "Name") == name);
        }
        public bool CheckExistByUserName(string name)
        {
            //return _context.Users.Any(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            return _context.Users
        .Any(u => u.Name.ToLower() == name.ToLower());
        }
        public async Task<T> GetByColumnAsync(string columnName, string value)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(e => EF.Property<string>(e, columnName) == value);
        }
        public async Task<T?> GetBySSNAsync(string SSN)
            => await _context.Set<T>().FirstOrDefaultAsync(e => EF.Property<string>(e, "SSN") == SSN);
        public T GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }
        public Task<T> GetByUserIdAsync(int UserId)
        {
            return _context.Set<T>().FirstAsync(x => EF.Property<int>(x, "UserId") == UserId);
        }
        public bool CheckExistById(int id)
        {
            return _context.Set<T>().Any(x => EF.Property<int>(x, "Id") == id);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public List<string> GetDistinct(Expression<Func<T, string>> col)
        {
            var distinctValues = _context.Set<T>()
                                           .Select(col.Compile())
                                           .Distinct()
                                           .ToList();

            return distinctValues;
        }
        public T Find(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return query.SingleOrDefault(criteria);
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var incluse in includes)
                    query = query.Include(incluse);

            return await query.SingleOrDefaultAsync(criteria);
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return query.Where(criteria).ToList();
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int skip, int take)
        {
            return _context.Set<T>().Where(criteria).Skip(skip).Take(take).ToList();
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int? skip, int? take, Expression<Func<T, object>> orderBy = null, bool IsDesc = false)
        {
            IQueryable<T> query = _context.Set<T>().Where(criteria);

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (take.HasValue)
                query = query.Take(take.Value);

            if (orderBy != null)
            {
                if (!IsDesc)
                    query = query.OrderBy(orderBy);
                else
                    query = query.OrderByDescending(orderBy);
            }

            return query.ToList();
        }

        /// <summary>
        /// used with Jquery datatable
        /// </summary>
        /// <param name="criteria"> condtions</param>
        /// <param name="sortColumn"> sort columns of datatable</param>
        /// <param name="sortColumnDirection"> Asc or Desc</param>
        /// <param name="skip">for paging </param>
        /// <param name="take">for paging</param>
        /// <returns>The records based on the current page</returns>
        public IEnumerable<T> FindWithFilters(Expression<Func<T, bool>> criteria = null, string sortColumn = null, string sortColumnDirection = null, int? skip = null, int? take = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (criteria != null)
                query = _context.Set<T>().Where(criteria);

            //if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            //{
            //    query = query.OrderBy(string.Concat(sortColumn, " ", sortColumnDirection));
            //}

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return query.ToList();
        }

        public async Task<List<User>> GetUsersWithUserRoles()
        {
            return await _context.Users.Include(r => r.UserRoles).ThenInclude(ur => ur.Role).ToListAsync();
        }

        public async Task<IQueryable<User>> GetUsersWithCategoryAndRole() => 
            _context.Users.Include(C => C.Supplier).ThenInclude(w => w.SupplierCategories).ThenInclude(p=>p.Category)
            .Include(r => r.UserRoles).ThenInclude(ur => ur.Role).Include(x=>x.JopSeeker).ThenInclude(x=>x.JopSeekerCategoryApplies).ThenInclude(c=>c.Category);
        public async Task<IQueryable<Order>>GetOrdersWithClientNameAndCategory()
        {
            return _context.Orders.Include(o => o.User).ThenInclude(cc=>cc.Supplier).ThenInclude(x=>x.SupplierCategories).ThenInclude(k=>k.Category).Include(i=>i.Items).AsQueryable();
        }
        public async Task<IQueryable<User>> GetSuppliersWithPlan() => _context.Users.Include(C => C.Supplier).ThenInclude(w => w.SupplierSubscriptionPlan).ThenInclude(p => p.SubscriptionPlan)
             .Include(s => s.Supplier).ThenInclude(c => c.SupplierCategories).ThenInclude(k => k.Category)
            .Include(s=>s.Supplier).ThenInclude(d=>d.Deals);

        public async Task<IQueryable<Category>> GetCategoriesWithCompanyAndUser()
        {
            return _context.Categories.Include(c => c.SupplierCategorys).ThenInclude(cc => cc.Supplier).ThenInclude(u => u.User).ThenInclude(k=>k.UserRoles).ThenInclude(i=>i.Role);
        }
        public async Task<IQueryable<SupplierAdvertisement>> GetAdvertisementsWithSupplierAndUser()
        {
            return _context.SupplierAdvertisements
                .Include(ad => ad.Supplier)
                .ThenInclude(s => s.User)
                .AsQueryable();
        }
        public async Task<IQueryable<Review>> GetAllReviewsWithClientsAsync()
        {
            return _context.Reviews
            .Include(r => r.Reviewer)
            .AsQueryable();
        }
        public async Task<IQueryable<SupplierProductRequest>> GetAllProductRequestWithSupplierAsync()
        {
            return _context.SupplierProductRequests
            .Include(r=>r.Supplier).ThenInclude(s=>s.User)
            .AsQueryable();
        }
        public async Task<IQueryable<SupplierAdvertisementRequest>> GetAllAdvertisementRequestWithSupplierAsync()
        {
            return _context.SupplierAdvertisementRequests
            .Include(r => r.Supplier).ThenInclude(s => s.User)
            .AsQueryable();
        }
        public async Task<IQueryable<SupplierAcceptOrderRequest>> GetAllAcceptOrderRequestWithSupplierAsync()
        {
            return _context.SupplierAcceptOrderRequests
            .Include(r => r.Supplier).ThenInclude(s => s.User)
            .AsQueryable();
        }
        public async Task<IQueryable<Blog>> GetAllPostsAsync()
        {
            return _context.Blogs.Where(b=>true).AsQueryable();
        }
        public async Task<IEnumerable<T>> FindWithFiltersAsync(
        int queryId,
        Expression<Func<T, bool>> criteria = null,
        string sortColumn = null,
        string sortColumnDirection = null,
        int? skip = null,
        int? take = null)
        {
            IQueryable<T> query;

            switch (queryId)
            {
                case 1:
                    query = (IQueryable<T>)await GetUsersWithCategoryAndRole();
                    break;
                case 2:
                    query = (IQueryable<T>)await GetOrdersWithClientNameAndCategory();
                    break;
                case 3:
                    {
                        var userQuery = (IQueryable<User>)await GetSuppliersWithPlan();

                        userQuery = userQuery
         
                            .OrderBy(e => e.Supplier.SupplierSubscriptionPlan == null ? 1 : 0)

         
                            .ThenByDescending(e => e.Supplier.SupplierSubscriptionPlan != null
                                && e.Supplier.SupplierSubscriptionPlan.ShowHigherInSearch)

         
                            .ThenBy(e =>
                                e.Supplier.SupplierSubscriptionPlan == null ? 99 :
                                e.Supplier.SupplierSubscriptionPlan.PlanId == 5 ? 0 :
                                e.Supplier.SupplierSubscriptionPlan.PlanId == 6 ? 1 :
                                e.Supplier.SupplierSubscriptionPlan.PlanId == 4 ? 2 : 3)

         
                            .ThenBy(e => e.Supplier.SupplierSubscriptionPlan != null
                                ? e.Supplier.SupplierSubscriptionPlan.PlanId
                                : 99)

                            .ThenBy(e => e.Id);

                        query = (IQueryable<T>)userQuery;
                        break;
                    }
                case 4:
                    query = (IQueryable<T>)await GetCategoriesWithCompanyAndUser();
                    break;
                case 5:
                    query = (IQueryable<T>)await GetAdvertisementsWithSupplierAndUser();
                    break;
                case 6:
                    query = (IQueryable<T>)await GetAllReviewsWithClientsAsync();
                    break;
                case 7:
                    query = (IQueryable<T>)await GetAllProductRequestWithSupplierAsync();
                    break;
                case 8:
                    query = (IQueryable<T>)await GetAllAdvertisementRequestWithSupplierAsync();
                    break;
                case 9:
                    query = (IQueryable<T>)await GetAllAcceptOrderRequestWithSupplierAsync();
                    break;
                case 10:
                    query = (IQueryable<T>)await GetAllPostsAsync();
                    break;
                default:
                    throw new ArgumentException("Invalid queryId");
            }

            if (criteria != null)
                query = query.Where(criteria);

            // Apply normal sorting only if queryId != 3 (since case 3 already has custom ordering)
            if (!string.IsNullOrEmpty(sortColumn) && queryId != 3)
            {
                query = sortColumnDirection == "Desc"
                    ? query.OrderByDescending(e => EF.Property<object>(e, sortColumn))
                    : query.OrderBy(e => EF.Property<object>(e, sortColumn));
            }

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (take.HasValue)
                query = query.Take(take.Value);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsyncWithoutCraiteria(string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.Where(criteria).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, int take, int skip)
        {
            return await _context.Set<T>().Where(criteria).Skip(skip).Take(take).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            int? take,
            int? skip,
            Expression<Func<T, object>> orderBy = null,
            bool isDesc = false)
        {
            IQueryable<T> query = _context.Set<T>().Where(criteria);

            // 1. ORDERING MUST COME FIRST
            if (orderBy != null)
            {
                query = isDesc
                    ? query.OrderByDescending(orderBy)
                    : query.OrderBy(orderBy);
            }
            else if (skip.HasValue || take.HasValue)
            {
                throw new InvalidOperationException("Ordering is required for pagination");
            }

            // 2. SKIP BEFORE TAKE
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        public T Add(T entity)
        {
            _context.Set<T>().Add(entity);
            return entity;
        }

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            return entity;
        }

        public IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            _context.Set<T>().AddRange(entities);
            return entities;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
            return entities;
        }

        public T Update(T entity)
        {
            _context.Update(entity);

            return entity;
        }
        public bool UpdateRange(IEnumerable<T> entities)
        {
            _context.UpdateRange(entities);

            return true;
        }
        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public int Count()
        {
            return _context.Set<T>().Count();
        }

        public int Count(Expression<Func<T, bool>> criteria)
        {
            return _context.Set<T>().Count(criteria);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Set<T>().CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> criteria)
        {
            return await _context.Set<T>().CountAsync(criteria);
        }
        public async Task<Int64> MaxAsync(Expression<Func<T, object>> column)
        {
            return Convert.ToInt64(await _context.Set<T>().MaxAsync(column));
        }
        public async Task<Int64> MaxAsync(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> column)
        {
            return Convert.ToInt64(await _context.Set<T>().Where(criteria).MaxAsync(column));
        }
        public Int64 Max(Expression<Func<T, object>> column)
        {
            return Convert.ToInt64(_context.Set<T>().Max(column));
        }
        public Int64 Max(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> column)
        {
            return Convert.ToInt64(_context.Set<T>().Where(criteria).Max(column));
        }
        public bool IsExist(Expression<Func<T, bool>> criteria)
        {
            return _context.Set<T>().Any(criteria);
        }
        public bool CheckExistByCompanyName(string Name)
        {
            return _context.Users.Any(x => x.Name.Equals(Name, StringComparison.CurrentCultureIgnoreCase));
        }
        public bool CheckExistByEmail(string Email)
        {
            return _context.Set<T>().Any(x => EF.Property<string>(x, "Email") == Email);
        }
        public T Last(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> orderBy)
        {
            return _context.Set<T>().OrderByDescending(orderBy).FirstOrDefault(criteria);
        }
    }
}

