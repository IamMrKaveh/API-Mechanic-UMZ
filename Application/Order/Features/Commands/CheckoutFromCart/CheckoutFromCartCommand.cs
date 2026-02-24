namespace Application.Order.Features.Commands.CheckoutFromCart;

public record CheckoutFromCartCommand : IRequest<ServiceResult<CheckoutResultDto>>
{
    public int UserId { get; init; }
    public int? UserAddressId { get; init; }
    public CreateUserAddressDto? NewAddress { get; init; }
    public bool SaveNewAddress { get; init; }
    public int ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CheckoutItemPriceDto> ExpectedItems { get; init; } = new();
    public string? CallbackUrl { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string GatewayName { get; init; } = "Zarinpal";
}