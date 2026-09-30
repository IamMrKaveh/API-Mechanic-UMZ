using Application.Attribute.Features.Shared;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public class CreateAttributeValueHandler(
    IAttributeRepository repository,
    IMapper mapper,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateAttributeValueCommand, AttributeValueDto>
{
    public async Task<ServiceResult<AttributeValueDto>> Handle(
        CreateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.TypeId);

        var typeResult = await (repository.GetAttributeTypeWithValuesAsync(attributeTypeId, ct)).OrNotFoundAsync("Attribute type not found.");
        if (typeResult.IsFailure) return typeResult.Error;
        var type = typeResult.Value;

        if (await repository.AttributeValueExistsAsync(attributeTypeId, request.Value, null, ct))
            return ServiceResult<AttributeValueDto>.Conflict("Attribute value already exists.");

        var attributeValue = type.AddValue(
            request.Value,
            request.DisplayValue,
            dateTimeProvider.UtcNow,
            request.HexCode,
            request.SortOrder);

        await repository.UpdateAttributeTypeAsync(type, ct);

        return ServiceResult<AttributeValueDto>.Success(mapper.Map<AttributeValueDto>(attributeValue));
    }
}