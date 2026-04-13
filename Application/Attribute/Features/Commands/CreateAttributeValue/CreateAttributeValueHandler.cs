using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public class CreateAttributeValueHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateAttributeValueCommand, ServiceResult<AttributeValueDto>>
{
    public async Task<ServiceResult<AttributeValueDto>> Handle(
        CreateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.TypeId);

        var type = await repository.GetAttributeTypeWithValuesAsync(attributeTypeId, ct);
        if (type is null)
            return ServiceResult<AttributeValueDto>.NotFound("Attribute type not found.");

        if (await repository.AttributeValueExistsAsync(attributeTypeId, request.Value, null, ct))
            return ServiceResult<AttributeValueDto>.Conflict("Attribute value already exists.");

        var attributeValue = type.AddValue(
            request.Value,
            request.DisplayValue,
            request.HexCode,
            request.SortOrder);

        await repository.UpdateAttributeTypeAsync(type, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<AttributeValueDto>.Success(mapper.Map<AttributeValueDto>(attributeValue));
    }
}