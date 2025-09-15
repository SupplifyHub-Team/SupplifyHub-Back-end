namespace API.Services
{
    /// <summary>
    /// Background service that runs once daily at 12 PM (UTC) to handle subscription-related tasks.  
    /// 
    /// Responsibilities:  
    /// 1. Calculates the next run time (12 PM UTC) and delays execution until then.  
    /// 2. Deletes products and advertisements that belong to suppliers with expired subscription plans.  
    /// 3. Sends email reminders to suppliers whose subscriptions are about to expire,  
    ///    prompting them to update their plans.  
    /// 4. Uses a scoped instance of <see cref="SubscriptionService"/> for each run  
    ///    to ensure proper lifecycle management of the database context.  
    /// 5. Handles cancellation requests gracefully and logs any exceptions that occur during execution.  
    /// 
    /// This service runs continuously until the application stops.  
    /// </summary>
    public class ScheduledForSubscriptionPlan : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ScheduledForSubscriptionPlan> _logger;

        public ScheduledForSubscriptionPlan(IServiceProvider services, ILogger<ScheduledForSubscriptionPlan> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Scheduled Token Cleanup is starting.");

            // Loop indefinitely until the application is stopped.
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate the time until the next 12 PM.
                var now = DateTime.UtcNow;
                var nextRunTime = now.Date.AddDays(now.Hour >= 12 ? 1 : 0).AddHours(12);
                var delay = nextRunTime - now;

                // Wait until the scheduled time.
                //_logger.LogInformation("Next token cleanup is scheduled for {NextRunTime}.", nextRunTime);
                await Task.Delay(delay, stoppingToken);

                // Check if the cancellation was requested while waiting.
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                //_logger.LogInformation("Running token cleanup service.");

                try
                {
                    // Create a new scope to get a new instance of the service.
                    // This is crucial for correctly managing database context lifecycles.
                    using (var scope = _services.CreateScope())
                    {
                        var cleaner = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
                        await cleaner.DeleteProductAndAdsForExpiredPlansAsync();
                        await cleaner.SendEmailToSupplierToUpdateSubscriptionAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning tokens.");
                }
            }
            //_logger.LogInformation("Scheduled Token Cleanup is stopping.");
        }
    }

}