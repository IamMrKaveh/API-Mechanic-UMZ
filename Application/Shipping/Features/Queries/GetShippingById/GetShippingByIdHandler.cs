namespace Application.Shipping.Features.Queries.GetShippingById;

public class GetShippingByIdHandler : IRequestHandler<GetShippingByIdQuery, ServiceResult<ShippingDto>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetShippingByIdHandler(
        IShippingQueryService shippingQueryService
        ) => _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<ShippingDto>> Handle(
        GetShippingByIdQuery request,
        CancellationToken ct
        )
    {
        var shipping = await _shippingQueryService.GetShippingByIdAsync(request.Id, ct);
        if (shipping == null)
            return ServiceResult<ShippingDto>.Failure("NotFound");
        return ServiceResult<ShippingDto>.Success(shipping);
    }
}