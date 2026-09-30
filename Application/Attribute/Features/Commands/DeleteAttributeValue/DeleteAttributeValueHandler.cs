using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public class DeleteAttributeValueHandler(
    IAttributeRepository repository)
    : ICommandHandler<DeleteAttributeValueCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValueId = AttributeValueId.From(request.Id);

        var attributeValueResult = await (repository.GetAttributeValueByIdAsync(attributeValueId, ct)).OrNotFoundAsync("Attribute value not found.");
        if (attributeValueResult.IsFailure) return attributeValueResult.Error;
        var attributeValue = attributeValueResult.Value;

        await repository.DeleteAttributeValueAsync(attributeValue.Id, null, ct);

        return ServiceResult.Success();
    }
}