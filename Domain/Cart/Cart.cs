namespace Domain.Cart;

public class Cart : AggregateRoot, ISoftDeletable, IAuditable
{
    private readonly List<CartItem> _cartItems = new();

    public int? UserId { get; private set; }
    public string? GuestToken { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public User.User? User { get; private set; }

    public IReadOnlyCollection<CartItem> CartItems => _cartItems.AsReadOnly();

    // Computed Properties
    public decimal TotalPrice => _cartItems.Sum(x => x.TotalPrice);

    public int TotalItems => _cartItems.Sum(x => x.Quantity);
    public bool IsEmpty => _cartItems.Count == 0;
    public bool IsGuestCart => !UserId.HasValue && !string.IsNullOrEmpty(GuestToken);
    public bool IsUserCart => UserId.HasValue;
    public bool HasItems => _cartItems.Count > 0;

    // Business Rules
    private const int MaxCartItems = 50;

    private const int MaxQuantityPerItem = 1000;

    private Cart()
    { }

    #region Factory Methods

    public static Cart CreateForUser(int userId)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        return new Cart
        {
            UserId = userId,
            GuestToken = null,
            LastUpdated = DateTime.UtcNow
        };
    }

    public static Cart CreateForGuest(string guestToken)
    {
        Guard.Against.NullOrWhiteSpace(guestToken, nameof(guestToken));

        return new Cart
        {
            UserId = null,
            GuestToken = guestToken.Trim(),
            LastUpdated = DateTime.UtcNow
        };
    }

    public static Cart CreateForGuest()
    {
        var token = ValueObjects.GuestToken.Create();
        return CreateForGuest(token.Value);
    }

    public static Cart Create(int? userId, string? guestToken)
    {
        if (userId.HasValue)
            return CreateForUser(userId.Value);

        if (!string.IsNullOrWhiteSpace(guestToken))
            return CreateForGuest(guestToken);

        throw new DomainException("سبد خرید باید به کاربر یا مهمان تعلق داشته باشد.");
    }

    #endregion Factory Methods

    #region Item Management - Core Business Logic

    /// <summary>
    /// افزودن آیتم به سبد - فقط ساختار سبد را مدیریت می‌کند.
    /// بررسی موجودی مسئولیت Application Layer است.
    /// </summary>
    public CartItem AddItem(int variantId, int quantity, decimal unitPrice)
    {
        EnsureNotDeleted();
        EnsureValidQuantity(quantity);
        EnsureValidPrice(unitPrice);

        var existingItem = FindItemByVariant(variantId);

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + quantity;
            EnsureValidItemQuantity(newQuantity);

            existingItem.UpdateQuantity(newQuantity);
            existingItem.UpdatePrice(unitPrice);
            TouchLastUpdated();

            return existingItem;
        }

        EnsureCanAddMoreItems();

        var newItem = CartItem.Create(this, variantId, quantity, unitPrice);
        _cartItems.Add(newItem);
        TouchLastUpdated();

        AddDomainEvent(new CartItemAddedEvent(Id, variantId, quantity));

        return newItem;
    }

    /// <summary>
    /// به‌روزرسانی تعداد آیتم
    /// </summary>
    public void UpdateItemQuantity(int variantId, int newQuantity)
    {
        EnsureNotDeleted();

        if (newQuantity <= 0)
        {
            RemoveItem(variantId);
            return;
        }

        EnsureValidItemQuantity(newQuantity);

        var item = GetItemOrThrow(variantId);
        var oldQuantity = item.Quantity;

        item.UpdateQuantity(newQuantity);
        TouchLastUpdated();

        AddDomainEvent(new CartItemUpdatedEvent(Id, variantId, oldQuantity, newQuantity));
    }

    /// <summary>
    /// حذف آیتم از سبد
    /// </summary>
    public void RemoveItem(int variantId)
    {
        EnsureNotDeleted();

        var item = FindItemByVariant(variantId);
        if (item == null)
            throw new CartItemNotFoundException(Id, variantId);

        _cartItems.Remove(item);
        TouchLastUpdated();

        AddDomainEvent(new CartItemRemovedEvent(Id, variantId));
    }

    /// <summary>
    /// پاک کردن کامل سبد
    /// </summary>
    public void Clear()
    {
        EnsureNotDeleted();

        if (IsEmpty)
            return;

        _cartItems.Clear();
        TouchLastUpdated();

        AddDomainEvent(new CartClearedEvent(Id));
    }

    /// <summary>
    /// به‌روزرسانی قیمت یک آیتم - فراخوانی از Application Layer هنگام Sync
    /// </summary>
    public void UpdateItemPrice(int variantId, decimal newPrice)
    {
        EnsureNotDeleted();
        EnsureValidPrice(newPrice);

        var item = GetItemOrThrow(variantId);
        item.UpdatePrice(newPrice);
        TouchLastUpdated();
    }

    #endregion Item Management - Core Business Logic

    #region User Assignment & Merge

    /// <summary>
    /// اختصاص سبد مهمان به کاربر
    /// </summary>
    public void AssignToUser(int userId)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        if (UserId.HasValue)
            throw new InvalidCartOperationException(Id, "AssignToUser", "سبد قبلاً به کاربر اختصاص داده شده.");

        UserId = userId;
        GuestToken = null;
        TouchLastUpdated();
    }

    /// <summary>
    /// ادغام سبد مهمان با سبد کاربر
    /// </summary>
    public void MergeWith(Cart sourceCart, CartMergeStrategy strategy = CartMergeStrategy.KeepHigherQuantity)
    {
        Guard.Against.Null(sourceCart, nameof(sourceCart));

        if (sourceCart.Id == Id)
            throw new InvalidCartOperationException(Id, "MergeWith", "امکان ادغام سبد با خودش وجود ندارد.");

        EnsureNotDeleted();

        if (sourceCart.IsDeleted)
            throw new InvalidCartOperationException(sourceCart.Id, "MergeWith", "سبد مبدأ حذف شده است.");

        if (sourceCart.IsEmpty)
            return;

        foreach (var sourceItem in sourceCart.CartItems)
        {
            var existingItem = FindItemByVariant(sourceItem.VariantId);

            if (existingItem != null)
            {
                var mergedQuantity = CalculateMergedQuantity(
                    existingItem.Quantity,
                    sourceItem.Quantity,
                    strategy);

                existingItem.UpdateQuantity(Math.Min(mergedQuantity, MaxQuantityPerItem));

                // به‌روزرسانی قیمت با جدیدترین
                if (sourceItem.UpdatedAt > existingItem.UpdatedAt)
                {
                    existingItem.UpdatePrice(sourceItem.SellingPrice);
                }
            }
            else if (_cartItems.Count < MaxCartItems)
            {
                var newItem = CartItem.Create(
                    this,
                    sourceItem.VariantId,
                    Math.Min(sourceItem.Quantity, MaxQuantityPerItem),
                    sourceItem.SellingPrice);
                _cartItems.Add(newItem);
            }
        }

        TouchLastUpdated();
        AddDomainEvent(new CartMergedEvent(Id, sourceCart.Id));
    }

    private static int CalculateMergedQuantity(int current, int source, CartMergeStrategy strategy)
    {
        return strategy switch
        {
            CartMergeStrategy.KeepHigherQuantity => Math.Max(current, source),
            CartMergeStrategy.SumQuantities => current + source,
            CartMergeStrategy.KeepUserCart => current,
            CartMergeStrategy.KeepGuestCart => source,
            _ => Math.Max(current, source)
        };
    }

    #endregion User Assignment & Merge

    #region Soft Delete

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        TouchLastUpdated();
    }

    #endregion Soft Delete

    #region Query Methods

    public CartItem? FindItemByVariant(int variantId)
    {
        return _cartItems.FirstOrDefault(x => x.VariantId == variantId);
    }

    public bool ContainsVariant(int variantId)
    {
        return _cartItems.Any(x => x.VariantId == variantId);
    }

    public int GetItemQuantity(int variantId)
    {
        return FindItemByVariant(variantId)?.Quantity ?? 0;
    }

    public IEnumerable<int> GetVariantIds()
    {
        return _cartItems.Select(x => x.VariantId);
    }

    #endregion Query Methods

    #region Domain Invariants

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidCartOperationException(Id, "Operation", "سبد خرید حذف شده است.");
    }

    private static void EnsureValidQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید بزرگتر از صفر باشد.");
    }

    private void EnsureValidItemQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید بزرگتر از صفر باشد.");

        if (quantity > MaxQuantityPerItem)
            throw new DomainException($"حداکثر تعداد مجاز برای هر آیتم {MaxQuantityPerItem} عدد است.");
    }

    private static void EnsureValidPrice(decimal price)
    {
        if (price < 0)
            throw new DomainException("قیمت نمی‌تواند منفی باشد.");
    }

    private void EnsureCanAddMoreItems()
    {
        if (_cartItems.Count >= MaxCartItems)
            throw new DomainException($"حداکثر تعداد آیتم‌های مجاز در سبد {MaxCartItems} عدد است.");
    }

    private CartItem GetItemOrThrow(int variantId)
    {
        var item = FindItemByVariant(variantId);
        if (item == null)
            throw new CartItemNotFoundException(Id, variantId);
        return item;
    }

    private void TouchLastUpdated()
    {
        LastUpdated = DateTime.UtcNow;
    }

    #endregion Domain Invariants
}