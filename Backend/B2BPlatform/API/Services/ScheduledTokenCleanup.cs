namespace API.Services
{
    /// <summary>
    /// Background service that periodically performs cleanup tasks in the system.  
    /// 
    /// Responsibilities:  
    /// 1. Runs in the background and executes every 1 hour.  
    /// 2. Cleans up expired tokens to keep authentication data valid.  
    /// 3. Cleans up expired advertisements for admins.  
    /// 4. Creates a scoped instance of <see cref="CleanupService"/> for each execution,  
    ///    ensuring proper lifecycle management of the database context.  
    /// 5. Logs errors if any exceptions occur during cleanup.  
    /// 
    /// This service runs continuously until the application stops.  
    /// </summary>
    public class ScheduledTokenCleanup : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ScheduledTokenCleanup> _logger;

        public ScheduledTokenCleanup(IServiceProvider services, ILogger<ScheduledTokenCleanup> logger)
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
                //_logger.LogInformation("Running token cleanup service.");

                try
                {
                    // Create a new scope to get a new instance of the service.
                    // This is crucial for correctly managing database context lifecycles.
                    using (var scope = _services.CreateScope())
                    {
                        var cleaner = scope.ServiceProvider.GetRequiredService<CleanupService>();
                        await cleaner.CleanExpiredTokensAsync();
                        await cleaner.CleanExpiredAdvertisementForAdminAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning tokens.");
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            //_logger.LogInformation("Scheduled Token Cleanup is stopping.");
        }
    }

}