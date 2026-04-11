using Application.Attribute.Adapters;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAttributeTypeCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var attributeType = await repository.GetAttributeTypeByIdAsync(attributeTypeId, ct);
        if (attributeType is null)
            return ServiceResult.NotFound("Attribute type not found.");

        var uniquenessChecker = new AttributeTypeUniquenessCheckerAdapter(repository);

        attributeType.Update(
            request.Name ?? attributeType.Name,
            request.DisplayName ?? attributeType.DisplayName,
            request.SortOrder ?? attributeType.SortOrder,
            request.IsActive ?? attributeType.IsActive,
            uniquenessChecker);

        await repository.UpdateAttributeTypeAsync(attributeType, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}