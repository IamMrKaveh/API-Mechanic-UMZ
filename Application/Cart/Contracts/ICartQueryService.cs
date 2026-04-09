using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Contracts;

public interface ICartQueryService
{
    Task<CartDetailDto?> GetCartDetailAsync(
        UserId? userId,
        GuestToken? guestToken,
        CancellationToken ct = default);

    Task<CartSummaryDto> GetCartSummaryAsync(
        UserId? userId,
        GuestToken? guestToken,
        CancellationToken ct = default);

    Task<CartCheckoutValidationDto> ValidateCartForCheckoutAsync(
        UserId? userId,
        GuestToken? guestToken,
        CancellationToken ct = default);
}