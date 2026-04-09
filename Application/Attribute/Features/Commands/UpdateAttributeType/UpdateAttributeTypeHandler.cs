using Application.Attribute.Adapters;
using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAttributeTypeCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        UpdateAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var attributeType = await _repository.GetAttributeTypeByIdAsync(attributeTypeId, ct);
        if (attributeType is null)
            return ServiceResult.NotFound("Attribute type not found.");

        var uniquenessChecker = new AttributeTypeUniquenessCheckerAdapter(_repository);

        attributeType.Update(
            request.Name ?? attributeType.Name,
            request.DisplayName ?? attributeType.DisplayName,
            request.SortOrder ?? attributeType.SortOrder,
            request.IsActive ?? attributeType.IsActive,
            uniquenessChecker);

        await _repository.UpdateAttributeTypeAsync(attributeType);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}