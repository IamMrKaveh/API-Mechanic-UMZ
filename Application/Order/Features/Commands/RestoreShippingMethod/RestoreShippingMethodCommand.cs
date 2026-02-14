namespace Application.Order.Features.Commands.RestoreShippingMethod;

public record RestoreShippingMethodCommand(int Id, int CurrentUserId) : IRequest<ServiceResult>;