using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Cart.Repositories;

public sealed class CartRepository(DBContext context) : ICartRepository
{
    public async Task<Domain.Cart.Aggregates.Cart?> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        return await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    }

    public async Task<Domain.Cart.Aggregates.Cart?> GetByGuestTokenAsync(GuestToken token, CancellationToken ct = default)
    {
        return await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.GuestToken == token, ct);
    }

    public async Task<Domain.Cart.Aggregates.Cart?> GetByIdAsync(CartId cartId, CancellationToken ct = default)
    {
        return await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);
    }

    public async Task AddAsync(Domain.Cart.Aggregates.Cart cart, CancellationToken ct = default)
    {
        await context.Carts.AddAsync(cart, ct);
    }

    public void Update(Domain.Cart.Aggregates.Cart cart)
    {
        context.Carts.Update(cart);
    }

    public async Task<bool> ExistsByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        return await context.Carts.AnyAsync(c => c.UserId == userId, ct);
    }

    public async Task<bool> ExistsByGuestTokenAsync(GuestToken token, CancellationToken ct = default)
    {
        return await context.Carts.AnyAsync(c => c.GuestToken == token, ct);
    }
}