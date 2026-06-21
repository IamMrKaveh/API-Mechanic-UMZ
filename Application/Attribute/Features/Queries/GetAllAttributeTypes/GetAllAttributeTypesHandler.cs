using Application.Attribute.Constants;
using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler(
    IAttributeRepository repository,
    IMapper mapper,
    ICacheService cacheService)
    : IQueryHandler<GetAllAttributeTypesQuery, PaginatedResult<AttributeTypeDto>>
{
    public async Task<ServiceResult<PaginatedResult<AttributeTypeDto>>> Handle(
        GetAllAttributeTypesQuery request,
        CancellationToken ct)
    {
        var cached = await cacheService.GetAsync<PaginatedResult<AttributeTypeDto>>(AttributeCacheKeys.AllTypes, ct);
        if (cached is not null && cached.TotalCount > 0)
            return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(cached);

        if (cached is not null)
            await cacheService.RemoveAsync(AttributeCacheKeys.AllTypes, ct);

        var types = await repository.GetAllAttributeTypesAsync(ct);
        var dtos = mapper.Map<List<AttributeTypeDto>>(types);

        var page = 1;
        var pageSize = dtos.Count > 0 ? dtos.Count : 1;
        var result = new PaginatedResult<AttributeTypeDto>(dtos, dtos.Count, page, pageSize);

        await cacheService.SetAsync(AttributeCacheKeys.AllTypes, result, TimeSpan.FromHours(1), ct);

        return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(result);
    }
}