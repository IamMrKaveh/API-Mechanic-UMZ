using Application.Common.Results;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public class UpdateAttributeValueHandler : IRequestHandler<UpdateAttributeValueCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAttributeValueHandler(
        IAttributeRepository repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        UpdateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValue = await _repository.GetAttributeValueByIdAsync(request.Id);
        if (attributeValue == null)
            return ServiceResult.NotFound("Attribute value not found.");

        if (request.Value != null && await _repository.AttributeValueExistsAsync(attributeValue.AttributeTypeId, request.Value, request.Id))
            return ServiceResult.Conflict("Attribute value already exists.");

        attributeValue.Update(
                    request.Value ?? attributeValue.Value,
                    request.DisplayValue ?? attributeValue.DisplayValue,
                    request.HexCode ?? attributeValue.HexCode,
                    request.SortOrder ?? attributeValue.SortOrder,
                    request.IsActive ?? attributeValue.IsActive
                );

        await _repository.UpdateAttributeValueAsync(attributeValue);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}