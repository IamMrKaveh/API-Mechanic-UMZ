using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public record GetAttributeTypeByIdQuery(Guid Id) : ICommand<AttributeTypeDto>;