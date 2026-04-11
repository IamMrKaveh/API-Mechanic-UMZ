using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public class DeleteAttributeTypeHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteAttributeTypeCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        DeleteAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var attributeType = await repository.GetAttributeTypeByIdAsync(attributeTypeId, ct);
        if (attributeType is null)
            return ServiceResult.NotFound("Attribute type not found.");

        await repository.DeleteAttributeTypeAsync(attributeType.Id, null, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}