using Application.Attribute.Adapters;
using Application.Attribute.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public class CreateAttributeTypeHandler(
    IAttributeRepository repository,
    IMapper mapper,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateAttributeTypeCommand, ServiceResult<AttributeTypeDto>>
{
    public async Task<ServiceResult<AttributeTypeDto>> Handle(
        CreateAttributeTypeCommand request,
        CancellationToken ct)
    {
        if (await repository.AttributeTypeExistsAsync(request.Name, null, ct))
            return ServiceResult<AttributeTypeDto>.Conflict("Attribute type already exists.");

        var uniquenessChecker = new AttributeTypeUniquenessCheckerAdapter(repository);
        var attributeType = AttributeType.Create(
            request.Name,
            request.DisplayName,
            request.SortOrder,
            true,
            uniquenessChecker);

        await repository.AddAttributeTypeAsync(attributeType, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<AttributeTypeDto>.Success(mapper.Map<AttributeTypeDto>(attributeType));
    }
}