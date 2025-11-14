namespace Infrastructure.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly LedkaContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(LedkaContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }
}