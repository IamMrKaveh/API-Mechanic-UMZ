using Application.Attribute.Adapters;
using Application.Attribute.Constants;
using Application.Attribute.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public class CreateAttributeTypeHandler(
    IAttributeRepository repository,
    IMapper mapper,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateAttributeTypeCommand, AttributeTypeDto>
{
    public async Task<ServiceResult<AttributeTypeDto>> Handle(
        CreateAttributeTypeCommand request,
        CancellationToken ct)
    {
        var uniquenessChecker = new AttributeTypeUniquenessCheckerAdapter(repository);
        var attributeType = await AttributeType.Create(
            request.Name,
            request.DisplayName,
            request.SortOrder,
            true,
            uniquenessChecker,
            dateTimeProvider.UtcNow,
            ct);

        await repository.AddAttributeTypeAsync(attributeType, ct);
        await cacheService.RemoveAsync(AttributeCacheKeys.AllTypes, ct);

        return ServiceResult<AttributeTypeDto>.Success(mapper.Map<AttributeTypeDto>(attributeType));
    }
}