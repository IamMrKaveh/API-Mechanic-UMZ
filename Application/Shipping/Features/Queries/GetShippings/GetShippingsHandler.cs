using Application.Common.Results;

namespace Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsHandler(
    IShippingQueryService shippingQueryService,
    IMapper mapper) : IRequestHandler<GetShippingsQuery, ServiceResult<IEnumerable<ShippingDto>>>
{
    private readonly IShippingQueryService _shippingQueryService = shippingQueryService;
    private readonly IMapper _mapper = mapper;

    public async Task<ServiceResult<IEnumerable<ShippingDto>>> Handle(
        GetShippingsQuery request,
        CancellationToken ct)
    {
        var shippings = await _shippingQueryService.GetAllAsync(request.IncludeDeleted, ct);
        var dtos = _mapper.Map<IEnumerable<ShippingDto>>(shippings);
        return ServiceResult<IEnumerable<ShippingDto>>.Success(dtos);
    }
}