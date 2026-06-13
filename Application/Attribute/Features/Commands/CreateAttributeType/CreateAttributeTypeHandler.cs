using Application.Attribute.Adapters;
using Application.Attribute.Constants;
using Application.Attribute.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public class CreateAttributeTypeHandler(
    IAttributeRepository repository,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<CreateAttributeTypeCommand, ServiceResult<AttributeTypeDto>>
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
            ct);

        await repository.AddAttributeTypeAsync(attributeType, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(AttributeCacheKeys.AllTypes, ct);

        return ServiceResult<AttributeTypeDto>.Success(mapper.Map<AttributeTypeDto>(attributeType));
    }
}