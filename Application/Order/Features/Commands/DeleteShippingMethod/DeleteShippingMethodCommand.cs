namespace Application.Order.Features.Commands.DeleteShippingMethod;

public record DeleteShippingMethodCommand(int Id, int CurrentUserId) : IRequest<ServiceResult>;