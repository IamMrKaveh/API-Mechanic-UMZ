using Application.Attribute.Adapters;
using Application.Attribute.Constants;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandler(
    IAttributeRepository repository,
    ICacheService cacheService)
    : ICommandHandler<UpdateAttributeTypeCommand>
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

        await attributeType.Update(
            request.Name ?? attributeType.Name,
            request.DisplayName ?? attributeType.DisplayName,
            request.SortOrder ?? attributeType.SortOrder,
            request.IsActive ?? attributeType.IsActive,
            uniquenessChecker,
            ct);

        await repository.UpdateAttributeTypeAsync(attributeType, ct);
        await cacheService.RemoveAsync(AttributeCacheKeys.AllTypes, ct);

        return ServiceResult.Success();
    }
}