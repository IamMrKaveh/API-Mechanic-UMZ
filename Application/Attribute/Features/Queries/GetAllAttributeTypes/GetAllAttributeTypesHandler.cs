using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler(
    IAttributeRepository repository,
    IMapper mapper,
    ICacheService cacheService) : IRequestHandler<GetAllAttributeTypesQuery, ServiceResult<PaginatedResult<AttributeTypeDto>>>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<ServiceResult<PaginatedResult<AttributeTypeDto>>> Handle(
        GetAllAttributeTypesQuery request,
        CancellationToken ct)
    {
        const string cacheKey = "attributes:all_types";

        var cached = await _cacheService.GetAsync<PaginatedResult<AttributeTypeDto>>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(cached);

        var types = await _repository.GetAllAttributeTypesAsync(ct);
        var dtos = _mapper.Map<PaginatedResult<AttributeTypeDto>>(types);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1));

        return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(dtos);
    }
}