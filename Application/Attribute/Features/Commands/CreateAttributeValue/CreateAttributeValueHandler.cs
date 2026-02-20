using Application.Attribute.Contracts;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public class CreateAttributeValueHandler : IRequestHandler<CreateAttributeValueCommand, ServiceResult<AttributeValueDto>>
{
    private readonly IAttributeRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAttributeValueHandler(IAttributeRepository repository, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<AttributeValueDto>> Handle(CreateAttributeValueCommand request, CancellationToken cancellationToken)
    {
        var type = await _repository.GetAttributeTypeWithValuesAsync(request.TypeId, cancellationToken); // Ensure this repo method exists or use GetAttributeTypeByIdAsync
        if (type == null)
            return ServiceResult<AttributeValueDto>.Failure("Attribute type not found.");
        if (await _repository.AttributeValueExistsAsync(request.TypeId, request.Value))
        {
            return ServiceResult<AttributeValueDto>.Failure("Attribute value already exists.");
        }

        var attributeValue = type.AddValue(request.Value, request.DisplayValue, request.HexCode, request.SortOrder);

        await _repository.AddAttributeValueAsync(attributeValue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AttributeValueDto>.Success(_mapper.Map<AttributeValueDto>(attributeValue));
    }
}