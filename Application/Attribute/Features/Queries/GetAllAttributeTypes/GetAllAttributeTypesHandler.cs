using Application.Attribute.Features.Shared;
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

        var page = 1;
        var pageSize = dtos.Count > 0 ? dtos.Count : 1;
        var result = new PaginatedResult<AttributeTypeDto>(dtos, dtos.Count, page, pageSize);

        return ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(result);
    }
}