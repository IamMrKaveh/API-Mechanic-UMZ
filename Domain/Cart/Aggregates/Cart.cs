using Domain.Cart.Entities;
using Domain.Cart.Enum;
using Domain.Cart.Events;
using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Aggregates;

public sealed class Cart : AggregateRoot<CartId>
{
    private readonly List<CartItem> _items = [];

    public UserId? UserId { get; private set; }
    public GuestToken? GuestToken { get; private set; }
    public bool IsCheckedOut { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart()
    { }

    private Cart(CartId id, UserId? userId, GuestToken? guestToken) : base(id)
    {
        UserId = userId;
        GuestToken = guestToken;
        IsCheckedOut = false;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CartCreatedEvent(id, userId, guestToken));
    }

    public static Cart CreateForUser(UserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return new Cart(CartId.NewId(), userId, null);
    }

    public static Cart CreateForGuest(GuestToken guestToken)
    {
        ArgumentNullException.ThrowIfNull(guestToken);
        return new Cart(CartId.NewId(), null, guestToken);
    }

    public void AddItem(
        VariantId variantId,
        ProductId productId,
        ProductName productName,
        Sku sku,
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
        IncrementVersion();

        RaiseDomainEvent(new CartItemAddedEvent(Id, variantId, productId, productName, quantity, unitPrice.Amount));
    }

    public void RemoveItem(VariantId variantId)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new CartItemRemovedEvent(Id, variantId, item.Quantity));
    }

    public void UpdateItemQuantity(VariantId variantId, int quantity)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        item.UpdateQuantity(quantity);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void RefreshItemPrice(VariantId variantId, Money newUnitPrice, Money newOriginalPrice)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        item.RefreshPrice(newUnitPrice, newOriginalPrice);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void Clear()
    {
        EnsureNotCheckedOut();
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void Checkout()
    {
        EnsureNotCheckedOut();

        if (_items.Count == 0)
            throw new InvalidOperationException(string.Empty);

        IsCheckedOut = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var total = _items.Sum(i => i.TotalPrice.Amount);
        RaiseDomainEvent(new CartCheckedOutEvent(Id, UserId, _items.Count, total));
    }

    public void AssignToUser(UserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        UserId = userId;
        GuestToken = null;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MergeFrom(Cart sourceCart, CartMergeStrategy strategy = CartMergeStrategy.SumQuantities)
    {
        EnsureNotCheckedOut();

        if (UserId is null)
            throw new InvalidOperationException(string.Empty);

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
        IncrementVersion();

        RaiseDomainEvent(new CartMergedEvent(Id, sourceCart.Id, UserId!, sourceCart.Items.Count));
    }

    public void ValidateStockAvailability(
        VariantId variantId,
        int requestedQuantity,
        int availableStock,
        bool isUnlimited)
    {
        if (requestedQuantity <= 0)
            throw new InvalidCartQuantityException(requestedQuantity);

        if (!isUnlimited && availableStock < requestedQuantity)
            throw new InsufficientStockForCartException(variantId, requestedQuantity, availableStock);
    }

    public bool HasItem(VariantId variantId) => _items.Any(i => i.VariantId == variantId);

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