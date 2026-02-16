namespace Application.Order.Features.Queries.GetShippingMethodById;

public class GetShippingMethodByIdHandler : IRequestHandler<GetShippingMethodByIdQuery, ServiceResult<ShippingMethodDto>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetShippingMethodByIdHandler(IShippingQueryService shippingQueryService) => _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<ShippingMethodDto>> Handle(GetShippingMethodByIdQuery request, CancellationToken ct)
    {
        var method = await _shippingQueryService.GetShippingMethodByIdAsync(request.Id, ct);
        if (method == null) return ServiceResult<ShippingMethodDto>.Failure("NotFound");
        return ServiceResult<ShippingMethodDto>.Success(method);
    }
}