using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler(
    IAttributeRepository repository,
    IMapper mapper,
    ICacheService cacheService) : IRequestHandler<GetAllAttributeTypesQuery, ServiceResult<PaginatedResult<AttributeTypeDto>>>
{
    public async Task<ServiceResult<PaginatedResult<AttributeTypeDto>>> Handle(
        GetAllAttributeTypesQuery request,
        CancellationToken ct)
    {
        const string cacheKey = "attributes:all_types";

        var cached = await cacheService.GetAsync<PaginatedResult<AttributeTypeDto>>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(cached);

        var types = await repository.GetAllAttributeTypesAsync(ct);
        var dtos = mapper.Map<PaginatedResult<AttributeTypeDto>>(types);

        await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1), ct);

        return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(dtos);
    }
}