using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart;

public record CheckoutFromCartCommand(
    Guid CartId,
    Guid ShippingId,
    Guid AddressId,
    string? DiscountCode,
    string? PaymentMethod,
    Guid? PaymentMethodId,
    Guid IdempotencyKey)
    : ICommand<CheckoutResultDto>, IIdempotentCommand
{
    public Guid UserId { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
}