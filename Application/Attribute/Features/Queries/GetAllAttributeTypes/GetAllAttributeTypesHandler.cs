using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler(
    IAttributeRepository repository,
    IMapper mapper,
    ICacheService cacheService) : IRequestHandler<GetAllAttributeTypesQuery, ServiceResult<IEnumerable<AttributeTypeDto>>>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<ServiceResult<IEnumerable<AttributeTypeDto>>> Handle(
        GetAllAttributeTypesQuery request,
        CancellationToken cancellationToken
        )
    {
        const string cacheKey = "attributes:all_types";

        var cached = await _cacheService.GetAsync<IEnumerable<AttributeTypeDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return ServiceResult<IEnumerable<AttributeTypeDto>>.Success(cached);
        }

        var types = await _repository.GetAllAttributeTypesAsync(cancellationToken);
        var dtos = _mapper.Map<IEnumerable<AttributeTypeDto>>(types);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1));

        return ServiceResult<IEnumerable<AttributeTypeDto>>.Success(dtos);
    }
}