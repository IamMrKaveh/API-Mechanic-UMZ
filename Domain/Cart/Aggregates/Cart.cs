using Domain.Cart.Entities;
using Domain.Cart.Enum;
using Domain.Cart.Events;
using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;

namespace Domain.Cart.Aggregates;

public sealed class Cart : AggregateRoot<Guid>
{
    private readonly List<CartItem> _items = [];

    public Guid? UserId { get; private set; }
    public GuestToken? GuestToken { get; private set; }
    public bool IsCheckedOut { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart()
    { }

    private Cart(Guid id, Guid? userId, GuestToken? guestToken) : base(id)
    {
        UserId = userId;
        GuestToken = guestToken;
        IsCheckedOut = false;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CartCreatedEvent(id, userId, guestToken?.Value));
    }

    public static Cart CreateForUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        return new Cart(Guid.NewGuid(), userId, null);
    }

    public static Cart CreateForGuest(GuestToken guestToken)
    {
        ArgumentNullException.ThrowIfNull(guestToken);
        return new Cart(Guid.NewGuid(), null, guestToken);
    }

    public void AddItem(
        Guid variantId,
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        Money originalPrice,
        int quantity)
    {
        EnsureNotCheckedOut();

        var existing = _items.FirstOrDefault(i => i.VariantId == variantId);

        if (existing is not null)
        {
            existing.IncrementQuantity(quantity);
        }
        else
        {
            var item = CartItem.Create(Id, variantId, productId, productName, sku, unitPrice, originalPrice, quantity);
            _items.Add(item);
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CartItemAddedEvent(Id, variantId, productId, productName, quantity, unitPrice.Amount));
    }

    public void RemoveItem(Guid variantId)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CartItemRemovedEvent(Id, variantId, item.Quantity));
    }

    public void UpdateItemQuantity(Guid variantId, int quantity)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        item.UpdateQuantity(quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        EnsureNotCheckedOut();
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Checkout()
    {
        EnsureNotCheckedOut();

        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot checkout an empty cart.");

        IsCheckedOut = true;
        UpdatedAt = DateTime.UtcNow;

        var total = _items.Sum(i => i.TotalPrice.Amount);
        RaiseDomainEvent(new CartCheckedOutEvent(Id, UserId, _items.Count, total));
    }

    public void AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        UserId = userId;
        GuestToken = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MergeFrom(Cart sourceCart, CartMergeStrategy strategy = CartMergeStrategy.SumQuantities)
    {
        EnsureNotCheckedOut();

        if (!UserId.HasValue)
            throw new InvalidOperationException("Target cart must belong to an authenticated user.");

        ArgumentNullException.ThrowIfNull(sourceCart);

        switch (strategy)
        {
            case CartMergeStrategy.KeepUserCart:
                break;

            case CartMergeStrategy.KeepGuestCart:
                _items.Clear();
                foreach (var sourceItem in sourceCart.Items)
                {
                    _items.Add(CartItem.Create(
                        Id,
                        sourceItem.VariantId,
                        sourceItem.ProductId,
                        sourceItem.ProductName,
                        sourceItem.Sku,
                        sourceItem.UnitPrice,
                        sourceItem.OriginalPrice,
                        sourceItem.Quantity));
                }
                break;

            case CartMergeStrategy.KeepHigherQuantity:
                foreach (var sourceItem in sourceCart.Items)
                {
                    var existing = _items.FirstOrDefault(i => i.VariantId == sourceItem.VariantId);
                    if (existing is not null)
                    {
                        if (sourceItem.Quantity > existing.Quantity)
                            existing.UpdateQuantity(sourceItem.Quantity);
                    }
                    else
                    {
                        _items.Add(CartItem.Create(
                            Id,
                            sourceItem.VariantId,
                            sourceItem.ProductId,
                            sourceItem.ProductName,
                            sourceItem.Sku,
                            sourceItem.UnitPrice,
                            sourceItem.OriginalPrice,
                            sourceItem.Quantity));
                    }
                }
                break;

            case CartMergeStrategy.SumQuantities:
            default:
                foreach (var sourceItem in sourceCart.Items)
                {
                    AddItem(
                        sourceItem.VariantId,
                        sourceItem.ProductId,
                        sourceItem.ProductName,
                        sourceItem.Sku,
                        sourceItem.UnitPrice,
                        sourceItem.OriginalPrice,
                        sourceItem.Quantity);
                }
                break;
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CartMergedEvent(Id, sourceCart.Id, UserId!.Value, sourceCart.Items.Count));
    }

    public void ValidateStockAvailability(
        Guid variantId,
        int requestedQuantity,
        int availableStock,
        bool isUnlimited)
    {
        if (requestedQuantity <= 0)
            throw new InvalidCartQuantityException(requestedQuantity);

        if (!isUnlimited && availableStock < requestedQuantity)
            throw new InsufficientStockForCartException(variantId, requestedQuantity, availableStock);
    }

    public bool HasItem(Guid variantId) => _items.Any(i => i.VariantId == variantId);

    public bool IsEmpty => _items.Count == 0;

    public Money TotalAmount =>
        _items.Aggregate(
            Money.Zero(),
            (acc, item) => acc.Add(item.TotalPrice));

    private void EnsureNotCheckedOut()
    {
        if (IsCheckedOut)
            throw new CartAlreadyCheckedOutException(Id);
    }
}