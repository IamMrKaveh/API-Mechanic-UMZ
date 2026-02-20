namespace Application.Shipping.Features.Queries.GetShippingById;

public class GetShippingByIdHandler : IRequestHandler<GetShippingByIdQuery, ServiceResult<ShippingMethodDto>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetShippingByIdHandler(IShippingQueryService shippingQueryService) => _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<ShippingMethodDto>> Handle(GetShippingByIdQuery request, CancellationToken ct)
    {
        var method = await _shippingQueryService.GetShippingMethodByIdAsync(request.Id, ct);
        if (method == null) return ServiceResult<ShippingMethodDto>.Failure("NotFound");
        return ServiceResult<ShippingMethodDto>.Success(method);
    }
}