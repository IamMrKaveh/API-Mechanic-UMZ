namespace Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsHandler : IRequestHandler<GetShippingsQuery, ServiceResult<IEnumerable<ShippingDto>>>
{
    private readonly IShippingRepository _shippingRepository;
    private readonly IMapper _mapper;

    public GetShippingsHandler(
        IShippingRepository shippingRepository,
        IMapper mapper
        )
    {
        _shippingRepository = shippingRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<ShippingDto>>> Handle(
        GetShippingsQuery request,
        CancellationToken ct
        )
    {
        var s = await _shippingRepository.GetAllAsync(request.IncludeDeleted);
        var dtos = _mapper.Map<IEnumerable<ShippingDto>>(s);
        return ServiceResult<IEnumerable<ShippingDto>>.Success(dtos);
    }
}