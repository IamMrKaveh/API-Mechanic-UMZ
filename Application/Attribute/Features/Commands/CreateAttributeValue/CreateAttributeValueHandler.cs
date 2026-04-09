using Application.Attribute.Features.Shared;
using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public class CreateAttributeValueHandler(
    IAttributeRepository repository,
    IMapper mapper,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateAttributeValueCommand, ServiceResult<AttributeValueDto>>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<AttributeValueDto>> Handle(
        CreateAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeTypeId = AttributeTypeId.From(request.TypeId);

        var type = await _repository.GetAttributeTypeWithValuesAsync(attributeTypeId, ct);
        if (type is null)
            return ServiceResult<AttributeValueDto>.NotFound("Attribute type not found.");

        if (await _repository.AttributeValueExistsAsync(attributeTypeId, request.Value, null, ct))
            return ServiceResult<AttributeValueDto>.Conflict("Attribute value already exists.");

        var attributeValue = type.AddValue(
            request.Value,
            request.DisplayValue,
            request.HexCode,
            request.SortOrder);

        await _repository.UpdateAttributeTypeAsync(type);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<AttributeValueDto>.Success(_mapper.Map<AttributeValueDto>(attributeValue));
    }
}