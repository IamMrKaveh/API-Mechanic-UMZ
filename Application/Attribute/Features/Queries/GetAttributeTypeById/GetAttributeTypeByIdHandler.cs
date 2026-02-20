using Application.Attribute.Contracts;
using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public class GetAttributeTypeByIdHandler : IRequestHandler<GetAttributeTypeByIdQuery, ServiceResult<AttributeTypeDto?>>
{
    private readonly IAttributeRepository _repository;
    private readonly IMapper _mapper;

    public GetAttributeTypeByIdHandler(IAttributeRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<AttributeTypeDto?>> Handle(GetAttributeTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var type = await _repository.GetAttributeTypeByIdAsync(request.Id);
        if (type == null) return ServiceResult<AttributeTypeDto?>.Failure("Attribute type not found.");
        return ServiceResult<AttributeTypeDto?>.Success(_mapper.Map<AttributeTypeDto>(type));
    }
}