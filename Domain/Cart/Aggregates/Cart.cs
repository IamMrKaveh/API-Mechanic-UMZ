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

        RaiseDomainEvent(new CartCreatedEvent(id.Value, userId?.Value, guestToken?.Value));
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
        ProductVariantId variantId,
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
        RaiseDomainEvent(new CartItemAddedEvent(Id.Value, variantId.Value, productId.Value, productName.Value, quantity, unitPrice.Amount));
    }

    public void RemoveItem(ProductVariantId variantId)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId.Value);

        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CartItemRemovedEvent(Id.Value, variantId.Value, item.Quantity));
    }

    public void UpdateItemQuantity(ProductVariantId variantId, int quantity)
    {
        EnsureNotCheckedOut();

        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId.Value);

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
        RaiseDomainEvent(new CartCheckedOutEvent(Id.Value, UserId?.Value, _items.Count, total));
    }

    public void AssignToUser(UserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        UserId = userId;
        GuestToken = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MergeFrom(Cart sourceCart, CartMergeStrategy strategy = CartMergeStrategy.SumQuantities)
    {
        EnsureNotCheckedOut();

        if (UserId is null)
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
        RaiseDomainEvent(new CartMergedEvent(Id.Value, sourceCart.Id.Value, UserId!.Value, sourceCart.Items.Count));
    }

    public void ValidateStockAvailability(
        ProductVariantId variantId,
        int requestedQuantity,
        int availableStock,
        bool isUnlimited)
    {
        if (requestedQuantity <= 0)
            throw new InvalidCartQuantityException(requestedQuantity);

        if (!isUnlimited && availableStock < requestedQuantity)
            throw new InsufficientStockForCartException(variantId.Value, requestedQuantity, availableStock);
    }

    public bool HasItem(ProductVariantId variantId) => _items.Any(i => i.VariantId == variantId);

    public bool IsEmpty => _items.Count == 0;

    public Money TotalAmount =>
        _items.Aggregate(
            Money.Zero(),
            (acc, item) => acc.Add(item.TotalPrice));

    private void EnsureNotCheckedOut()
    {
        if (IsCheckedOut)
            throw new CartAlreadyCheckedOutException(Id.Value);
    }
}