namespace Application.Shipping.Features.Commands.DeleteShipping;

public record DeleteShippingCommand(int Id, int CurrentUserId) : IRequest<ServiceResult>;