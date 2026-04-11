namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public record DeleteAttributeTypeCommand(Guid Id) : IRequest<ServiceResult>;