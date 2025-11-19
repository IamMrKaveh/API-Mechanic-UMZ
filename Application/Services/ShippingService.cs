namespace Application.Services;

public class ShippingService(IShippingMethodRepository shippingMethodRepository, IMapper mapper) : IShippingService
{
    public async Task<IEnumerable<object>> GetActiveShippingMethodsAsync()
    {
        var methods = await shippingMethodRepository.GetActiveShippingMethodsAsync();
        return mapper.Map<IEnumerable<object>>(methods);
    }
}