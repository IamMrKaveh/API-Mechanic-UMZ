namespace Application.Order.Features.Queries.GetShippingMethods;

public class GetShippingMethodsHandler : IRequestHandler<GetShippingMethodsQuery, ServiceResult<IEnumerable<ShippingMethodDto>>>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IMapper _mapper;

    public GetShippingMethodsHandler(IShippingMethodRepository shippingMethodRepository, IMapper mapper)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> Handle(GetShippingMethodsQuery request, CancellationToken cancellationToken)
    {
        var methods = await _shippingMethodRepository.GetAllAsync(request.IncludeDeleted);
        var dtos = _mapper.Map<IEnumerable<ShippingMethodDto>>(methods);
        return ServiceResult<IEnumerable<ShippingMethodDto>>.Success(dtos);
    }
}