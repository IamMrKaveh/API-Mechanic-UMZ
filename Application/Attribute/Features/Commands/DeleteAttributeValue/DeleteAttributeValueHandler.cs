using Application.Attribute.Constants;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public class DeleteAttributeValueHandler(
    IAttributeRepository repository,
    ICacheService cacheService)
    : ICommandHandler<DeleteAttributeValueCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValueId = AttributeValueId.From(request.Id);

        var attributeValue = await repository.GetAttributeValueByIdAsync(attributeValueId, ct);
        if (attributeValue is null)
            return ServiceResult.NotFound("Attribute value not found.");

        await repository.DeleteAttributeValueAsync(attributeValue.Id, null, ct);
        await cacheService.RemoveAsync(AttributeCacheKeys.AllTypes, ct);

        return ServiceResult.Success();
    }
}