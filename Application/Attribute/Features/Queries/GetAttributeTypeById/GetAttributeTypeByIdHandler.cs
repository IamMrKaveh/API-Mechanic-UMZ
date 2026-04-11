using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public class GetAttributeTypeByIdHandler(
    IAttributeRepository repository,
    IMapper mapper) : IRequestHandler<GetAttributeTypeByIdQuery, ServiceResult<AttributeTypeDto>>
{
    public async Task<ServiceResult<AttributeTypeDto>> Handle(
        GetAttributeTypeByIdQuery request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var type = await repository.GetAttributeTypeByIdAsync(attributeTypeId, ct);

        if (type is null)
            return ServiceResult<AttributeTypeDto>.NotFound("Attribute type not found.");

        return ServiceResult<AttributeTypeDto>.Success(mapper.Map<AttributeTypeDto>(type));
    }
}