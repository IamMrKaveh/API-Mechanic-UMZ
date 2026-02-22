namespace Application.Attribute.Features.Commands.CreateAttributeType;

public class CreateAttributeTypeHandler : IRequestHandler<CreateAttributeTypeCommand, ServiceResult<AttributeTypeDto>>
{
    private readonly IAttributeRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAttributeTypeHandler(
        IAttributeRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<AttributeTypeDto>> Handle(
        CreateAttributeTypeCommand request,
        CancellationToken cancellationToken
        )
    {
        if (await _repository.AttributeTypeExistsAsync(request.Name))
        {
            return ServiceResult<AttributeTypeDto>.Failure("Attribute type already exists.");
        }

        var attributeType = AttributeType.Create(request.Name, request.DisplayName, request.SortOrder, true);

        await _repository.AddAttributeTypeAsync(attributeType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AttributeTypeDto>.Success(_mapper.Map<AttributeTypeDto>(attributeType));
    }
}