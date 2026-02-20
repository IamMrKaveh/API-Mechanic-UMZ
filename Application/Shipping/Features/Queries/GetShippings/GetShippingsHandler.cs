namespace Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsHandler : IRequestHandler<GetShippingsQuery, ServiceResult<IEnumerable<ShippingMethodDto>>>
{
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly IMapper _mapper;

    public GetShippingsHandler(IShippingRepository shippingMethodRepository, IMapper mapper)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> Handle(GetShippingsQuery request, CancellationToken cancellationToken)
    {
        var methods = await _shippingMethodRepository.GetAllAsync(request.IncludeDeleted);
        var dtos = _mapper.Map<IEnumerable<ShippingMethodDto>>(methods);
        return ServiceResult<IEnumerable<ShippingMethodDto>>.Success(dtos);
    }
}