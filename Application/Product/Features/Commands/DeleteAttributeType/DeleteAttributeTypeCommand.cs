namespace Application.Product.Features.Commands.DeleteAttributeType;

public record DeleteAttributeTypeCommand(int Id) : IRequest<ServiceResult>;