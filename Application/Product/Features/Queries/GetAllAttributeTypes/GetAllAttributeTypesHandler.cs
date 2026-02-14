namespace Application.Product.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler : IRequestHandler<GetAllAttributeTypesQuery, ServiceResult<IEnumerable<AttributeTypeDto>>>
{
    private readonly IAttributeRepository _repository;
    private readonly IMapper _mapper;

    public GetAllAttributeTypesHandler(IAttributeRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<AttributeTypeDto>>> Handle(GetAllAttributeTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await _repository.GetAllAttributeTypesAsync();
        return ServiceResult<IEnumerable<AttributeTypeDto>>.Success(_mapper.Map<IEnumerable<AttributeTypeDto>>(types));
    }
}