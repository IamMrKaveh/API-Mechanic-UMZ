using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Cart.Repositories;

public sealed class CartRepository(DBContext context) : ICartRepository
{
    public async Task<Domain.Cart.Aggregates.Cart?> FindByIdAsync(
        CartId cartId,
        CancellationToken ct = default)
        => await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

    public async Task<Domain.Cart.Aggregates.Cart?> FindByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
        => await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

    public async Task<Domain.Cart.Aggregates.Cart?> FindByGuestTokenAsync(
        GuestToken token,
        CancellationToken ct = default)
        => await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.GuestToken == token && !c.IsCheckedOut, ct);

    public void Add(Domain.Cart.Aggregates.Cart cart)
        => context.Carts.Add(cart);

    public void Update(Domain.Cart.Aggregates.Cart cart)
        => context.Carts.Update(cart);

    public void Remove(Domain.Cart.Aggregates.Cart cart)
        => context.Carts.Remove(cart);
}