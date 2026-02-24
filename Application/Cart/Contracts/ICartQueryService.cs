namespace Application.Cart.Contracts;

public interface ICartQueryService
{
    Task<CartDetailDto?> GetCartDetailAsync(
        int? userId,
        string? guestToken,
        CancellationToken ct = default
        );

    Task<CartSummaryDto> GetCartSummaryAsync(
        int? userId,
        string? guestToken,
        CancellationToken ct = default
        );

    Task<CartCheckoutValidationDto> ValidateCartForCheckoutAsync(
        int? userId,
        string? guestToken,
        CancellationToken ct = default
        );
}