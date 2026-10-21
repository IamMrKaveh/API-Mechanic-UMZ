using Application.Attribute.Features.Shared;
using Application.Common.Validation;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandler(
    IAttributeRepository repository,
    IMapper mapper)
    : IQueryHandler<GetAllAttributeTypesQuery, PaginatedResult<AttributeTypeDto>>
{
    public async Task<ServiceResult<PaginatedResult<AttributeTypeDto>>> Handle(
        GetAllAttributeTypesQuery request,
        CancellationToken ct)
    {
        var types = await repository.GetAllAttributeTypesAsync(ct);
        var dtos = mapper.Map<List<AttributeTypeDto>>(types);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, PaginationDefaults.MaxPageSize);

        var items = dtos
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = PaginatedResult<AttributeTypeDto>.Create(items, dtos.Count, page, pageSize);

        return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(result);
    }
}
