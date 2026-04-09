namespace Application.Order.Features.Commands.ConfirmDelivery;

public record ConfirmDeliveryCommand(Guid OrderId, Guid UserId) : IRequest<ServiceResult>;