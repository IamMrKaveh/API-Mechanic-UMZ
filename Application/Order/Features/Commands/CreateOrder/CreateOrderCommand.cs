using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid UserId,
    string ReceiverName,
    Guid UserAddressId,
    Guid ShippingId,
    string? DiscountCode,
    ICollection<AdminCreateOrderItemDto> OrderItems,
    string IdempotencyKey,
    Guid AdminUserId) : IRequest<ServiceResult<Guid>>;