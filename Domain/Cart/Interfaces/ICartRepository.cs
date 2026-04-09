using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Interfaces;

public interface ICartRepository
{
    Task<Aggregates.Cart?> FindByIdAsync(
        CartId cartId,
        CancellationToken ct = default);

    Task<Aggregates.Cart?> FindByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<Aggregates.Cart?> FindByGuestTokenAsync(
        GuestToken token,
        CancellationToken ct = default);

    void Add(Aggregates.Cart cart);

    void Update(Aggregates.Cart cart);

    void Remove(Aggregates.Cart cart);
}