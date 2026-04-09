using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart;

public record CheckoutFromCartCommand(
    Guid UserId,
    Guid CartId,
    Guid ShippingId,
    Guid AddressId,
    string? DiscountCode,
    string? PaymentMethod,
    string IpAddress,
    string? UserAgent,
    Guid IdempotencyKey) : IRequest<ServiceResult<CheckoutResultDto>>;