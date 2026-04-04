using Application.Common.Results;
using Application.Product.Features.Shared;
using Domain.Attribute.Interfaces;
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
        var type = await _repository.GetAttributeTypeWithValuesAsync(request.TypeId, ct);
        if (type == null)
            return ServiceResult<AttributeValueDto>.NotFound("Attribute type not found.");
        if (await _repository.AttributeValueExistsAsync(request.TypeId, request.Value))
            return ServiceResult<AttributeValueDto>.Conflict("Attribute value already exists.");

        var attributeValue = type.AddValue(request.Value, request.DisplayValue, request.HexCode, request.SortOrder);

        await _repository.AddAttributeValueAsync(attributeValue);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<AttributeValueDto>.Success(_mapper.Map<AttributeValueDto>(attributeValue));
    }
}