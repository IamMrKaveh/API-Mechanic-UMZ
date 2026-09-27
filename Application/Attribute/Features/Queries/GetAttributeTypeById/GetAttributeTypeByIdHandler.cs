using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public class GetAttributeTypeByIdHandler(
    IAttributeRepository repository,
    IMapper mapper)
    : ICommandHandler<GetAttributeTypeByIdQuery, AttributeTypeDto>
{
    public async Task<ServiceResult<AttributeTypeDto>> Handle(
        GetAttributeTypeByIdQuery request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var typeResult = await (repository.GetAttributeTypeWithValuesAsync(attributeTypeId, ct)).OrNotFoundAsync("Attribute type not found.");
        if (typeResult.IsFailure) return typeResult.Error;
        var type = typeResult.Value;

        return ServiceResult<AttributeTypeDto>.Success(mapper.Map<AttributeTypeDto>(type));
    }
}