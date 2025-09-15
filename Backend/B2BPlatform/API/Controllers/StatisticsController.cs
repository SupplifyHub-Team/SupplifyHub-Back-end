using API.DTO;
using API.DTO.Orders;
using API.DTO.Plans;
using API.DTO.Users;
using API.Factory;
using Controllers;
using Entities;
using Enum;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Linq;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/statistics")]
    [ApiController]
    public class StatisticsController : APIBaseController
    {
        public StatisticsController(IUnitOfWork unitOfWork) : base(unitOfWork) { }
        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves statistical data for all orders in the system.  
        /// It groups orders by their current status (e.g., Active, Completed, Canceled).  
        /// For each status, it calculates the total number of orders.  
        /// It also identifies how many of those orders were created this month.  
        /// The data is aggregated into a list of `OrderStatisticsDto` objects.  
        /// The result helps administrators track order distribution and monthly trends.  
        /// Returns the statistics in an `Ok` response with a standardized format.  
        /// </summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrdersStatistics()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();

            // If you don't have CreatedAt, you need to add it to the Order entity for "new this month" logic
            var now = DateTime.UtcNow.Month;
            var year = DateTime.UtcNow.Year;

            // Fix: Use System.Enum.GetValues instead of Enum.GetValues
            var statuses = System.Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();

            var stats = statuses.Select(status => new OrderStatisticsDto
            {
                Status = status.ToString(),
                TotalCount = orders.Count(o => o.OrderStatus == status),
                NewThisMonth = orders.Count(o => o.OrderStatus == status && o.CreatedAt.Year == year && o.CreatedAt.Month == now)
            }).ToList();

            return Ok(ResponseFactory.CreateGeneralResponse(stats));
        }
        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves statistical insights about users in the system.  
        /// It excludes administrators and categorizes users into Clients, Suppliers,  
        /// Jobseekers, Active Users, and Inactive Users.  
        /// For each category, it calculates the total number of users.  
        /// It also identifies how many new users joined in the current month.  
        /// Additionally, it computes the percentage of new users relative to the total.  
        /// The logic relies on user roles and the `CreatedAt` property for date checks.  
        /// Results are aggregated into a list of `UserStatisticsDto` objects.  
        /// The method returns an `Ok` response with the statistics in a structured format.  
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersStatistics()
        {
            var users =  await _unitOfWork.Users.GetUsersWithUserRoles();

            var now = DateTime.UtcNow.Month;
            var year = DateTime.UtcNow.Year;



            // Helper function to check role
            bool HasRole(User u, RoleName role) =>
                u.UserRoles != null && u.UserRoles.Any(ur => ur.Role.Name == role);

            // Exclude admins
            var filteredUsers = users.Where(u => !HasRole(u, RoleName.Admin)).ToList();
            // Active and Inactive users
            var ActiveUsers = filteredUsers.Where(u => u.IsActive).ToList();
            var InactiveUsers = filteredUsers.Where(u => !u.IsActive).ToList();
            // Clients: users with "client" role and a Company
            var clients = filteredUsers.Where(u => HasRole(u, RoleName.Clients) ).ToList();
            // Suppliers: users with "supplier" role and a Company
            var suppliers = filteredUsers.Where(u => HasRole(u, RoleName.Suppliers)).ToList();
            // Jobseekers: users with "jobseeker" role and an Individual
            var jobseekers = filteredUsers.Where(u => HasRole(u, RoleName.JobSeeker)).ToList();

            // For new users, you need a CreatedAt property on User
            int clientsNew = clients.Count(u => u.CreatedAt.Month >= now && u.CreatedAt.Year == year);
            int suppliersNew = suppliers.Count(u =>  u.CreatedAt.Month >= now && u.CreatedAt.Year == year);
            int jobseekersNew = jobseekers.Count(u =>  u.CreatedAt.Month >= now && u.CreatedAt.Year == year);
            int activeUsersNew = ActiveUsers.Count(u => u.CreatedAt.Month >= now && u.CreatedAt.Year == year);
            int inactiveUsersNew = InactiveUsers.Count(u => u.CreatedAt.Month >= now && u.CreatedAt.Year == year);
            var stats = new List<UserStatisticsDto>
            {

                new UserStatisticsDto
                {
                    Category = "Clients",
                    TotalCount = clients.Count,
                    NewThisMonth = clientsNew,
                    NewUserPercentage = clients.Count == 0 ? 0 : Math.Round((clientsNew * 100.0) / clients.Count, 2)
                },
                new UserStatisticsDto
                {
                    Category = "Suppliers",
                    TotalCount = suppliers.Count,
                    NewThisMonth = suppliersNew,
                    NewUserPercentage = suppliers.Count == 0 ? 0 : Math.Round((suppliersNew * 100.0) / suppliers.Count, 2)
                },
                new UserStatisticsDto
                {
                    Category = "Jobseekers",
                    TotalCount = jobseekers.Count,
                    NewThisMonth = jobseekersNew,
                    NewUserPercentage = jobseekers.Count == 0 ? 0 : Math.Round((jobseekersNew * 100.0) / jobseekers.Count, 2)
                },
                new UserStatisticsDto
                {
                    Category = "Active Users",
                    TotalCount = ActiveUsers.Count,
                    NewThisMonth = activeUsersNew,
                    NewUserPercentage = ActiveUsers.Count == 0 ? 0 : Math.Round((activeUsersNew * 100.0) / ActiveUsers.Count, 2)
                },
                new UserStatisticsDto
                {
                    Category = "Inactive Users",
                    TotalCount = InactiveUsers.Count,
                    NewThisMonth = inactiveUsersNew,
                    NewUserPercentage = InactiveUsers.Count == 0 ? 0 : Math.Round((inactiveUsersNew * 100.0) / InactiveUsers.Count, 2)
                }
            };

            return Ok(ResponseFactory.CreateGeneralResponse(stats));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves high-level statistics across the system.  
        /// It calculates total counts for orders, users, and categories.  
        /// It also determines how many new records were created this month  
        /// by comparing `CreatedAt` values with the start of the current month.  
        /// The results are wrapped inside a `GeneralStatisticsDto` object.  
        /// Data helps administrators monitor overall platform growth and activity.  
        /// Returns an `Ok` response containing the aggregated statistics.  
        /// </summary>

        [HttpGet("general")]
        public async Task<IActionResult> GetGeneralStatistics()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var users = await _unitOfWork.Users.GetAllAsync();
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var generalStats = new GeneralStatisticsDto
            {
                TotalOrders = orders.Count(),
                TotalUsers = users.Count(),
                TotalCategories = categories.Count(),
                NewOrdersThisMonth = orders.Count(o => o.CreatedAt >= monthStart),
                NewUsersThisMonth = users.Count(u => u.CreatedAt >= monthStart),
                NewCategoriesThisMonth = categories.Count(c => c.CreatedAt >= monthStart)
            };

            return Ok(ResponseFactory.CreateGeneralResponse(new List<GeneralStatisticsDto> {generalStats}));
        }
        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves monthly order statistics for a specific year.  
        /// It first filters all orders to include only those created in the given year.  
        /// Then it iterates over all 12 months of the year.  
        /// For each month, it counts how many orders were created.  
        /// The results are structured as a list of anonymous objects  
        /// containing the month number and the corresponding order count.  
        /// This provides administrators with insights into order trends per month.  
        /// Returns an `Ok` response with the aggregated monthly statistics.  
        /// </summary>
        [HttpGet("orders/{year}")]
        public async Task<IActionResult> GetOrdersStatistics([FromRoute] int year)
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();

            // Filter orders for the specified year
            var ordersInYear = orders.Where(o => o.CreatedAt.Year == year);

            // Group by month and count
            var monthlyStats = Enumerable.Range(1, 12)
                .Select(month => new
                {
                    Month = month,
                    OrderCount = ordersInYear.Count(o => o.CreatedAt.Month == month)
                })
                .ToList();

            return Ok(ResponseFactory.CreateGeneralResponse(monthlyStats));
        }

        /// **Function Summary:**  
        /// This method retrieves monthly order statistics for a specific year,  
        /// grouped by order status.  
        /// It loops through all 12 months and evaluates orders created in that month.  
        /// For each month, it iterates over all possible `OrderStatus` values.  
        /// It then counts how many orders fall under each status for that month.  
        /// The result is a list of objects containing month numbers and status counts.  
        /// This provides detailed insights into how orders progress across the year.  
        /// Returns an `Ok` response with the aggregated monthly status statistics.  
        /// </summary>
        [HttpGet("orders-status/{year}")]
        public async Task<IActionResult> GetOrdersStatisticsStatus([FromRoute] int year)
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var statuses = System.Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();

            var stats = Enumerable.Range(1, 12)
                .Select(month => new
                {
                    Month = month,
                    StatusCounts = statuses.Select(status => new
                    {
                        Status = status.ToString(),
                        Count = orders.Count(o => o.CreatedAt.Year == year && o.CreatedAt.Month == month && o.OrderStatus == status)
                    })
                })
                .ToList();

            return Ok(ResponseFactory.CreateGeneralResponse(stats));
        }

        /// <summary>
        /// **Function Summary:**  
        /// This method retrieves statistical insights about subscription plans.  
        /// It loads all company subscription plans along with related supplier  
        /// and plan details.  
        /// For each subscription plan, it groups records and calculates totals.  
        /// It also counts how many new subscriptions were created this month.  
        /// Additionally, it computes the percentage of new subscribers relative  
        /// to the total subscribers of the plan.  
        /// Returns an `Ok` response with a list of `SubscriptionPlanStatisticsDto` objects.  
        /// </summary>
        [HttpGet("plans")]
        public async Task<IActionResult> GetSubscriptionPlansStatistics()
        {
            // Get all company subscription plans with related data
            var allCompanySubscriptionPlans = await _unitOfWork.SupplierSubscriptionPlans.FindAllAsyncWithoutCraiteria(
                new[] { "Supplier", "SubscriptionPlan" }
            );

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            // Group by subscription plan to get statistics per plan
            var stats = allCompanySubscriptionPlans
                .GroupBy(csp => csp.SubscriptionPlan.Id)
                .Select(g => new SubscriptionPlanStatisticsDto
                {
                    PlanName = g.First().SubscriptionPlan.Name,
                    TotalCount = g.Count(),
                    NewThisMonth = g.Count(csp => csp.CreatedAt >= monthStart),
                    NewSubscriberPercentage = g.Count() == 0
                        ? 0
                        : Math.Round((g.Count(csp => csp.CreatedAt >= monthStart) * 100.0) / g.Count(), 2)
                })
                .ToList();

            return Ok(ResponseFactory.CreateGeneralResponse(stats));
        }
    }
}
