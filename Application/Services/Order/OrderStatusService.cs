using Application.Common.Interfaces.Cache;
using Application.Common.Interfaces.Order;

namespace Application.Services.Order;

public class OrderStatusService : IOrderStatusService
{
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderStatusService> _logger;

    private const string OrderStatusesCacheKey = "order_statuses:all";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

    public OrderStatusService(
        IOrderStatusRepository orderStatusRepository,
        ICacheService cacheService,
        ILogger<OrderStatusService> logger)
    {
        _orderStatusRepository = orderStatusRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync()
    {
        var cached = await _cacheService.GetAsync<List<OrderStatus>>(OrderStatusesCacheKey);
        if (cached != null)
        {
            return cached;
        }

        var statuses = (await _orderStatusRepository.GetOrderStatusesAsync()).ToList();

        await _cacheService.SetAsync(OrderStatusesCacheKey, statuses, CacheExpiry);

        return statuses;
    }

    public async Task<OrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        var statuses = await GetOrderStatusesAsync();
        return statuses.FirstOrDefault(s => s.Id == id);
    }
}