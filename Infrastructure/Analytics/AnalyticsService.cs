namespace Infrastructure.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly MechanicContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(MechanicContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }
}