namespace Application.Order.Features.Queries.GetAvailableShippingMethodsForVariants;

public class GetAvailableShippingMethodsForVariantsHandler : IRequestHandler<GetAvailableShippingMethodsForVariantsQuery, ServiceResult<IEnumerable<AvailableShippingMethodDto>>>
{
    public Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> Handle(GetAvailableShippingMethodsForVariantsQuery request, CancellationToken ct)
        => Task.FromResult(ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Success(new List<AvailableShippingMethodDto>()));
}