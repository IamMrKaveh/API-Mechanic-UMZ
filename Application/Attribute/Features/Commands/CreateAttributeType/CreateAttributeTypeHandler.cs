using Application.Attribute.Features.Shared;
using Application.Common.Results;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Common.Interfaces;

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
        if (await _repository.AttributeTypeExistsAsync(request.Name, ct: ct))
        {
            return ServiceResult<AttributeTypeDto>.Failure("Attribute type already exists.");
        }

        var attributeType = AttributeType.Create(request.Name, request.DisplayName, request.SortOrder, true);

        await _repository.AddAttributeTypeAsync(attributeType, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<AttributeTypeDto>.Success(_mapper.Map<AttributeTypeDto>(attributeType));
    }
}