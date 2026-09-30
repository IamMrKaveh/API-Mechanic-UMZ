using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public class DeleteAttributeTypeHandler(
    IAttributeRepository repository)
    : ICommandHandler<DeleteAttributeTypeCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var attributeTypeResult = await (repository.GetAttributeTypeByIdAsync(attributeTypeId, ct)).OrNotFoundAsync("Attribute type not found.");
        if (attributeTypeResult.IsFailure) return attributeTypeResult.Error;
        var attributeType = attributeTypeResult.Value;

        await repository.DeleteAttributeTypeAsync(attributeType.Id, null, ct);

        return ServiceResult.Success();
    }
}