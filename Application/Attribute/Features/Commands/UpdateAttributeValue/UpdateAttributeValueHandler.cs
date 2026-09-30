using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public class UpdateAttributeValueHandler(
    IAttributeRepository repository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAttributeValueCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValueId = AttributeValueId.From(request.Id);

        var attributeValueResult = await (repository.GetAttributeValueByIdAsync(attributeValueId, ct)).OrNotFoundAsync("Attribute value not found.");
        if (attributeValueResult.IsFailure) return attributeValueResult.Error;
        var attributeValue = attributeValueResult.Value;

        var resolvedValue = request.Value ?? attributeValue.Value;

        if (request.Value is not null)
        {
            var isDuplicate = await repository.AttributeValueExistsAsync(
                attributeValue.AttributeTypeId,
                resolvedValue,
                attributeValueId,
                ct);

            if (isDuplicate)
                return ServiceResult.Conflict("Attribute value already exists.");
        }

        var typeResult = await (repository.GetAttributeTypeWithValuesAsync(attributeValue.AttributeTypeId, ct)).OrNotFoundAsync("Attribute type not found.");
        if (typeResult.IsFailure) return typeResult.Error;
        var type = typeResult.Value;

        type.UpdateValue(
            attributeValueId,
            resolvedValue,
            request.DisplayValue ?? attributeValue.DisplayValue,
            request.HexCode ?? attributeValue.HexCode,
            request.SortOrder ?? attributeValue.SortOrder,
            request.IsActive ?? attributeValue.IsActive,
            dateTimeProvider.UtcNow);

        await repository.UpdateAttributeTypeAsync(type, ct);

        return ServiceResult.Success();
    }
}