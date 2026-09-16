using Domain.Cart.Entities;
using Domain.Cart.Enum;
using Domain.Cart.Events;
using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;
using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Aggregates;

public sealed class Cart : AggregateRoot<CartId>
{
    public GuestToken? GuestToken { get; private set; }
    public bool IsCheckedOut { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public UserId? UserId { get; private set; }
    public User.Aggregates.User? User { get; private set; }

    public DiscountCodeId? AppliedDiscountCodeId { get; private set; }
    public DiscountCode? AppliedDiscountCode { get; private set; }

    private readonly List<CartItem> _cartItems = [];
    public IReadOnlyCollection<CartItem> CartItems => _cartItems.AsReadOnly();

    private Cart()
    { }

    private Cart(CartId id, UserId? userId, GuestToken? guestToken, DateTime now) : base(id)
    {
        UserId = userId;
        GuestToken = guestToken;
        IsCheckedOut = false;
        CreatedAt = now;

        RaiseDomainEvent(new CartCreatedEvent(id, userId, guestToken));
    }

    public static Cart CreateForUser(UserId userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return new Cart(CartId.NewId(), userId, null, now);
    }

    public static Cart CreateForGuest(GuestToken guestToken, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(guestToken);
        return new Cart(CartId.NewId(), null, guestToken, now);
    }

    public void AddItem(
        VariantId variantId,
        ProductId productId,
        ProductName productName,
        Sku sku,
        Money unitPrice,
        Money originalPrice,
        int quantity,
        DateTime now)
    {
        EnsureNotCheckedOut();
        ArgumentNullException.ThrowIfNull(unitPrice);
        ArgumentNullException.ThrowIfNull(originalPrice);

        var existing = _cartItems.FirstOrDefault(i => i.VariantId == variantId);

        if (existing is not null)
        {
            existing.IncrementQuantity(quantity);
        }
        else
        {
            var item = CartItem.Create(Id, variantId, productId, productName, sku, unitPrice, originalPrice, quantity, now);
            _cartItems.Add(item);
        }

        UpdatedAt = now;
        IncrementVersion();

        RaiseDomainEvent(new CartItemAddedEvent(Id, variantId, productId, productName, quantity, unitPrice.Amount));
    }

    public void RemoveItem(VariantId variantId, DateTime now)
    {
        EnsureNotCheckedOut();

        var item = _cartItems.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        _cartItems.Remove(item);
        UpdatedAt = now;
        IncrementVersion();

        RaiseDomainEvent(new CartItemRemovedEvent(Id, variantId, item.Quantity));
    }

    public void UpdateItemQuantity(VariantId variantId, int quantity, DateTime now)
    {
        EnsureNotCheckedOut();

        var item = _cartItems.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        item.UpdateQuantity(quantity);
        UpdatedAt = now;
        IncrementVersion();
    }

    public void RefreshItemPrice(VariantId variantId, Money newUnitPrice, Money newOriginalPrice, DateTime now)
    {
        EnsureNotCheckedOut();
        ArgumentNullException.ThrowIfNull(newUnitPrice);
        ArgumentNullException.ThrowIfNull(newOriginalPrice);

        var item = _cartItems.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new CartItemNotFoundException(variantId);

        item.RefreshPrice(newUnitPrice, newOriginalPrice);
        UpdatedAt = now;
        IncrementVersion();
    }

    public void Clear(DateTime now)
    {
        EnsureNotCheckedOut();
        _cartItems.Clear();
        UpdatedAt = now;
        IncrementVersion();
    }

    public void Checkout(DateTime now)
    {
        EnsureNotCheckedOut();

        if (_cartItems.Count == 0)
            throw new InvalidOperationException(string.Empty);

        IsCheckedOut = true;
        UpdatedAt = now;
        IncrementVersion();

        var total = _cartItems.Sum(i => i.TotalPrice.Amount);
        RaiseDomainEvent(new CartCheckedOutEvent(Id, UserId, _cartItems.Count, total));
    }

    public void AssignToUser(UserId userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(userId);
        UserId = userId;
        GuestToken = null;
        UpdatedAt = now;
        IncrementVersion();
    }

    public void MergeFrom(Cart sourceCart, DateTime now, CartMergeStrategy strategy = CartMergeStrategy.SumQuantities)
    {
        EnsureNotCheckedOut();

        if (sourceCart is null)
            throw new InvalidOperationException("Source cart cannot be null.");

        if (UserId is null)
            throw new InvalidOperationException("Target cart must belong to a registered user.");

        switch (strategy)
        {
            case CartMergeStrategy.KeepUserCart:
                break;

            case CartMergeStrategy.KeepGuestCart:
                _cartItems.Clear();
                foreach (var sourceItem in sourceCart.CartItems)
                {
                    _cartItems.Add(CartItem.Create(
                        Id,
                        sourceItem.VariantId,
                        sourceItem.ProductId,
                        sourceItem.ProductName,
                        sourceItem.VariantSku,
                        sourceItem.SellingPrice,
                        sourceItem.OriginalPrice,
                        sourceItem.Quantity,
                        now));
                }
                break;

            case CartMergeStrategy.KeepHigherQuantity:
                foreach (var sourceItem in sourceCart.CartItems)
                {
                    var existing = _cartItems.FirstOrDefault(i => i.VariantId == sourceItem.VariantId);
                    if (existing is not null)
                    {
                        if (sourceItem.Quantity > existing.Quantity)
                            existing.UpdateQuantity(sourceItem.Quantity);
                    }
                    else
                    {
                        _cartItems.Add(CartItem.Create(
                            Id,
                            sourceItem.VariantId,
                            sourceItem.ProductId,
                            sourceItem.ProductName,
                            sourceItem.VariantSku,
                            sourceItem.SellingPrice,
                            sourceItem.OriginalPrice,
                            sourceItem.Quantity,
                            now));
                    }
                }
                break;

            case CartMergeStrategy.SumQuantities:
            default:
                foreach (var sourceItem in sourceCart.CartItems)
                {
                    AddItem(
                        sourceItem.VariantId,
                        sourceItem.ProductId,
                        sourceItem.ProductName,
                        sourceItem.VariantSku,
                        sourceItem.SellingPrice,
                        sourceItem.OriginalPrice,
                        sourceItem.Quantity,
                        now);
                }
                break;
        }

        UpdatedAt = now;
        IncrementVersion();

        RaiseDomainEvent(new CartMergedEvent(Id, sourceCart.Id, UserId!, sourceCart.CartItems.Count));
    }

    public bool IsEmpty => _cartItems.Count == 0;

    public Money TotalAmount =>
        _cartItems.Aggregate(
            Money.Zero(),
            (acc, item) => acc.Add(item.TotalPrice));

    private void EnsureNotCheckedOut()
    {
        if (IsCheckedOut)
            throw new CartAlreadyCheckedOutException(Id);
    }
}
