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
    private readonly IAttributeRepository _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<AttributeTypeDto>> Handle(
        CreateAttributeTypeCommand request,
        CancellationToken ct)
    {
        if (await _repository.AttributeTypeExistsAsync(request.Name, null, ct))
            return ServiceResult<AttributeTypeDto>.Conflict("Attribute type already exists.");

        var uniquenessChecker = new AttributeTypeUniquenessCheckerAdapter(_repository);
        var attributeType = AttributeType.Create(
            request.Name,
            request.DisplayName,
            request.SortOrder,
            true,
            uniquenessChecker);

        await _repository.AddAttributeTypeAsync(attributeType, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<AttributeTypeDto>.Success(_mapper.Map<AttributeTypeDto>(attributeType));
    }
}