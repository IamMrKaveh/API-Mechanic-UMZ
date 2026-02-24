namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public record DeleteAttributeTypeCommand(
    int Id
    ) : IRequest<ServiceResult>;