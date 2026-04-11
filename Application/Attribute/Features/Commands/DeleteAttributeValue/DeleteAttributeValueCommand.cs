namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public record DeleteAttributeValueCommand(Guid Id) : IRequest<ServiceResult>;