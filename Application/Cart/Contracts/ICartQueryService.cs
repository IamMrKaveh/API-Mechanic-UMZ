using Application.Cart.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Cart.Contracts;

public interface ICartQueryService
{
    Task<CartDetailDto?> GetCartDetailAsync(
        UserId? userId,
        string? guestToken,
        CancellationToken ct = default);

    Task<CartSummaryDto> GetCartSummaryAsync(
        UserId? userId,
        string? guestToken,
        CancellationToken ct = default);

    Task<CartCheckoutValidationDto> ValidateCartForCheckoutAsync(
        UserId? userId,
        string? guestToken,
        CancellationToken ct = default);
}