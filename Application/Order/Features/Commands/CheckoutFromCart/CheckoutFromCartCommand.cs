namespace Application.Features.Orders.Commands.CheckoutFromCart;

public record CheckoutFromCartCommand : IRequest<ServiceResult<CheckoutResultDto>>
{
    public int UserId { get; init; }
    public int? UserAddressId { get; init; }
    public CreateUserAddressDto? NewAddress { get; init; }
    public bool SaveNewAddress { get; init; }
    public int ShippingMethodId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CheckoutItemPriceDto> ExpectedItems { get; init; } = new();
    public string? CallbackUrl { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
}