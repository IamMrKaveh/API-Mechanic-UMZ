using Application.Attribute.Adapters;
using Application.Attribute.Constants;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandler(
    IAttributeRepository repository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAttributeTypeCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var attributeTypeResult = await (repository.GetAttributeTypeByIdAsync(attributeTypeId, ct)).OrNotFoundAsync("Attribute type not found.");
        if (attributeTypeResult.IsFailure) return attributeTypeResult.Error;
        var attributeType = attributeTypeResult.Value;

        var uniquenessChecker = new AttributeTypeUniquenessCheckerAdapter(repository);

        await attributeType.Update(
            request.Name ?? attributeType.Name,
            request.DisplayName ?? attributeType.DisplayName,
            request.SortOrder ?? attributeType.SortOrder,
            request.IsActive ?? attributeType.IsActive,
            uniquenessChecker,
            dateTimeProvider.UtcNow,
            ct);

        await repository.UpdateAttributeTypeAsync(attributeType, ct);
        await cacheService.RemoveAsync(AttributeCacheKeys.AllTypes, ct);

        return ServiceResult.Success();
    }
}