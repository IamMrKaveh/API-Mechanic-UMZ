namespace Application.Product.Features.Commands.DeleteAttributeValue;

public record DeleteAttributeValueCommand(int Id) : IRequest<ServiceResult>;