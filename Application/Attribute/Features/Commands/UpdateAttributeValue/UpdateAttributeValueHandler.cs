using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public class UpdateAttributeValueHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAttributeValueCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValueId = AttributeValueId.From(request.Id);

        var attributeValue = await repository.GetAttributeValueByIdAsync(attributeValueId, ct);
        if (attributeValue is null)
            return ServiceResult.NotFound("Attribute value not found.");

        if (request.Value is not null)
        {
            var isDuplicate = await repository.AttributeValueExistsAsync(
                attributeValue.AttributeTypeId,
                request.Value,
                attributeValueId,
                ct);

            if (isDuplicate)
                return ServiceResult.Conflict("Attribute value already exists.");
        }

        var type = await repository.GetAttributeTypeWithValuesAsync(attributeValue.AttributeTypeId, ct);
        if (type is null)
            return ServiceResult.NotFound("Attribute type not found.");

        type.UpdateValue(
            attributeValueId,
            attributeValue,
            request.DisplayValue ?? attributeValue.DisplayValue,
            request.HexCode ?? attributeValue.HexCode,
            request.SortOrder ?? attributeValue.SortOrder,
            request.IsActive ?? attributeValue.IsActive);

        await repository.UpdateAttributeTypeAsync(type, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}