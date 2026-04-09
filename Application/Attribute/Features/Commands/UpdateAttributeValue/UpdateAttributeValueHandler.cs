using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public class UpdateAttributeValueHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAttributeValueCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        UpdateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValueId = AttributeValueId.From(request.Id.Value);

        var attributeValue = await _repository.GetAttributeValueByIdAsync(attributeValueId, ct);
        if (attributeValue is null)
            return ServiceResult.NotFound("Attribute value not found.");

        if (request.Value is not null)
        {
            var isDuplicate = await _repository.AttributeValueExistsAsync(
                attributeValue.AttributeTypeId,
                request.Value,
                attributeValueId,
                ct);

            if (isDuplicate)
                return ServiceResult.Conflict("Attribute value already exists.");
        }

        var type = await _repository.GetAttributeTypeWithValuesAsync(attributeValue.AttributeTypeId, ct);
        if (type is null)
            return ServiceResult.NotFound("Attribute type not found.");

        type.UpdateValue(
            attributeValueId,
            request.Value ?? attributeValue,
            request.DisplayValue ?? attributeValue.DisplayValue,
            request.HexCode ?? attributeValue.HexCode,
            request.SortOrder ?? attributeValue.SortOrder,
            request.IsActive ?? attributeValue.IsActive);

        await _repository.UpdateAttributeTypeAsync(type);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}