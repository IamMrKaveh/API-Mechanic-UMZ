namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler : IRequestHandler<GetAllAttributeTypesQuery, ServiceResult<IEnumerable<AttributeTypeDto>>>
{
    private readonly IAttributeRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetAllAttributeTypesHandler(
        IAttributeRepository repository,
        IMapper mapper,
        ICacheService cacheService
        )
    {
        _repository = repository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<IEnumerable<AttributeTypeDto>>> Handle(
        GetAllAttributeTypesQuery request,
        CancellationToken cancellationToken
        )
    {
        const string cacheKey = "attributes:all_types";

        var cached = await _cacheService.GetAsync<IEnumerable<AttributeTypeDto>>(cacheKey);
        if (cached != null)
        {
            return ServiceResult<IEnumerable<AttributeTypeDto>>.Success(cached);
        }

        var types = await _repository.GetAllAttributeTypesAsync();
        var dtos = _mapper.Map<IEnumerable<AttributeTypeDto>>(types);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1));

        return ServiceResult<IEnumerable<AttributeTypeDto>>.Success(dtos);
    }
}