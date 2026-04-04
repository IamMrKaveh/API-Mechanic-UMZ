using Application.Attribute.Features.Shared;
using Application.Common.Results;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public class GetAttributeTypeByIdHandler(
    IAttributeRepository repository,
    IMapper mapper) : IRequestHandler<GetAttributeTypeByIdQuery, ServiceResult<AttributeTypeDto?>>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IMapper _mapper = mapper;

    public async Task<ServiceResult<AttributeTypeDto?>> Handle(
        GetAttributeTypeByIdQuery request,
        CancellationToken ct)
    {
        var type = await _repository.GetAttributeTypeByIdAsync(request.Id);
        if (type == null)
            return ServiceResult<AttributeTypeDto?>.NotFound("Attribute type not found.");
        return ServiceResult<AttributeTypeDto?>.Success(_mapper.Map<AttributeTypeDto>(type));
    }
}