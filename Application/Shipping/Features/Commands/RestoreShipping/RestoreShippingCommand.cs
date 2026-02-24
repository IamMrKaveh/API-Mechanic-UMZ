namespace Application.Shipping.Features.Commands.RestoreShipping;

public record RestoreShippingCommand(int Id, int CurrentUserId) : IRequest<ServiceResult>;