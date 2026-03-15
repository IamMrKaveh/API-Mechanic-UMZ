using Domain.Cart.ValueObjects;

namespace Domain.Cart.Interfaces;

public interface ICartRepository
{
    Task<Aggregates.Cart?> FindByIdAsync(Guid cartId, CancellationToken cancellationToken = default);

    Task<Aggregates.Cart?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Aggregates.Cart?> FindByGuestTokenAsync(GuestToken token, CancellationToken cancellationToken = default);

    void Add(Aggregates.Cart cart);

    void Update(Aggregates.Cart cart);

    void Remove(Aggregates.Cart cart);
}