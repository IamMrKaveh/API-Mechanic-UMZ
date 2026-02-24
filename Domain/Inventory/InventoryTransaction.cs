namespace Domain.Inventory;

public class InventoryTransaction : AggregateRoot, IAuditable
{
    private int _variantId;
    private string _transactionType = null!;
    private int _quantityChange;
    private int _stockBefore;
    private int? _orderItemId;
    private int? _userId;
    private string? _notes;
    private string? _referenceNumber;
    private bool _isReversed;
    private int? _reversedByTransactionId;

    
    private string? _correlationId;

    private string? _cartId;
    private DateTime? _expiresAt;

    public int VariantId => _variantId;
    public string TransactionType => _transactionType;
    public int QuantityChange => _quantityChange;
    public int StockBefore => _stockBefore;
    public int? OrderItemId => _orderItemId;
    public int? UserId => _userId;
    public string? Notes => _notes;
    public string? ReferenceNumber => _referenceNumber;
    public bool IsReversed => _isReversed;
    public int? ReversedByTransactionId => _reversedByTransactionId;

    /// <summary>
    /// کلید یکتا برای Idempotency - جلوگیری از ثبت مجدد در retry
    /// Index: (VariantId, TransactionType, CorrelationId)
    /// </summary>
    public string? CorrelationId => _correlationId;

    /// <summary>
    /// شناسه سبد خرید مرتبط (برای رزروهای cart-level)
    /// </summary>
    public string? CartId => _cartId;

    /// <summary>
    /// زمان انقضای رزرو (برای TTL-based cleanup)
    /// </summary>
    public DateTime? ExpiresAt => _expiresAt;

    
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    
    public ProductVariant? Variant { get; private set; }

    public OrderItem? OrderItem { get; private set; }
    public User.User? User { get; private set; }
    public ICollection<InventoryTransaction>? ReversalTransactions { get; private set; }

    
    public int StockAfter => _stockBefore + _quantityChange;

    private const int MaxNotesLength = 500;
    private const int MaxReferenceLength = 100;
    private const int MaxCorrelationIdLength = 200;

    private InventoryTransaction()
    { }

    #region Factory Methods

    public static InventoryTransaction Create(
        int variantId,
        TransactionType transactionType,
        int quantityChange,
        int stockBefore,
        int? userId = null,
        string? notes = null,
        string? referenceNumber = null,
        int? orderItemId = null,
        string? correlationId = null,
        string? cartId = null,
        DateTime? expiresAt = null)
    {
        Guard.Against.NegativeOrZero(variantId, nameof(variantId));
        ValidateNotes(notes);
        ValidateReferenceNumber(referenceNumber);
        ValidateCorrelationId(correlationId);

        return new InventoryTransaction
        {
            _variantId = variantId,
            _transactionType = transactionType.Value,
            _quantityChange = quantityChange,
            _stockBefore = stockBefore,
            _userId = userId,
            _notes = notes?.Trim(),
            _referenceNumber = referenceNumber?.Trim(),
            _orderItemId = orderItemId,
            _correlationId = correlationId?.Trim(),
            _cartId = cartId?.Trim(),
            _expiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            _isReversed = false
        };
    }

    public static InventoryTransaction CreateStockIn(
        int variantId,
        int quantity,
        int stockBefore,
        int? userId = null,
        string? notes = null,
        string? referenceNumber = null,
        string? correlationId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.StockIn,
            quantity,
            stockBefore,
            userId,
            notes ?? "افزایش موجودی",
            referenceNumber,
            correlationId: correlationId);

        transaction.AddDomainEvent(new AdjustStockEvent(variantId, transaction.StockAfter, quantity));
        return transaction;
    }

    public static InventoryTransaction CreateStockOut(
        int variantId,
        int quantity,
        int stockBefore,
        int? userId = null,
        string? notes = null,
        string? referenceNumber = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        EnsureSufficientStock(stockBefore, quantity);

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.StockOut,
            -quantity,
            stockBefore,
            userId,
            notes ?? "کاهش موجودی",
            referenceNumber);

        transaction.AddDomainEvent(new AdjustStockEvent(variantId, transaction.StockAfter, -quantity));
        transaction.CheckLowStockWarning();
        return transaction;
    }

    public static InventoryTransaction CreateReservation(
        int variantId,
        int quantity,
        int stockBefore,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        string? correlationId = null,
        string? cartId = null,
        DateTime? expiresAt = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NegativeOrZero(orderItemId, nameof(orderItemId));
        EnsureSufficientStock(stockBefore, quantity);

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Reservation,
            -quantity,
            stockBefore,
            userId,
            "رزرو موجودی برای سفارش",
            referenceNumber,
            orderItemId,
            correlationId: correlationId,
            cartId: cartId,
            expiresAt: expiresAt);

        transaction.AddDomainEvent(new StockReservedEvent(variantId, 0, quantity));
        return transaction;
    }

    /// <summary>
    /// تأیید رزرو پس از پرداخت موفق - کاهش Reserved و OnHand به‌صورت توأم
    /// از Sale تفکیک شده تا لجر انبار دقیق‌تر باشد
    /// </summary>
    public static InventoryTransaction CreateCommit(
        int variantId,
        int quantity,
        int stockBefore,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        string? correlationId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NegativeOrZero(orderItemId, nameof(orderItemId));

        
        var orderId = 0;
        if (referenceNumber?.StartsWith("ORDER-") == true &&
            int.TryParse(referenceNumber.Substring(6), out var parsed))
            orderId = parsed;

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Commit,
            -quantity,
            stockBefore,
            userId,
            "تأیید رزرو پس از پرداخت موفق",
            referenceNumber,
            orderItemId,
            correlationId: correlationId);

        transaction.AddDomainEvent(new StockCommittedEvent(variantId, orderId, quantity));
        transaction.CheckLowStockWarning();
        return transaction;
    }

    public static InventoryTransaction CreateSale(
        int variantId,
        int quantity,
        int stockBefore,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NegativeOrZero(orderItemId, nameof(orderItemId));

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Sale,
            -quantity,
            stockBefore,
            userId,
            "فروش محصول",
            referenceNumber,
            orderItemId);

        transaction.CheckLowStockWarning();
        return transaction;
    }

    public static InventoryTransaction CreateAdjustment(
        int variantId,
        int quantityChange,
        int stockBefore,
        int userId,
        string notes)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(notes, nameof(notes));

        var newStock = stockBefore + quantityChange;
        if (newStock < 0)
            throw new NegativeStockException(variantId, stockBefore, Math.Abs(quantityChange));

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Adjustment,
            quantityChange,
            stockBefore,
            userId,
            notes);

        transaction.AddDomainEvent(new AdjustStockEvent(variantId, transaction.StockAfter, quantityChange));
        transaction.CheckLowStockWarning();
        return transaction;
    }

    public static InventoryTransaction CreateReturn(
        int variantId,
        int quantity,
        int stockBefore,
        int orderItemId,
        int? userId = null,
        string? notes = null,
        string? correlationId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        
        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Return,
            quantity,
            stockBefore,
            userId,
            notes ?? "برگشت از فروش",
            orderItemId: orderItemId,
            correlationId: correlationId);

        transaction.AddDomainEvent(new StockRestoredEvent(variantId, 0, transaction.StockAfter));
        return transaction;
    }

    public static InventoryTransaction CreateDamage(
        int variantId,
        int quantity,
        int stockBefore,
        int userId,
        string notes)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(notes, nameof(notes));
        EnsureSufficientStock(stockBefore, quantity);

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Damage,
            -quantity,
            stockBefore,
            userId,
            notes);

        transaction.AddDomainEvent(new AdjustStockEvent(variantId, transaction.StockAfter, -quantity));
        transaction.CheckLowStockWarning();
        return transaction;
    }

    #endregion Factory Methods

    #region Domain Behaviors

    public InventoryTransaction CreateReversal(int? userId = null, string? notes = null)
    {
        EnsureCanBeReversed();

        var currentStock = StockAfter;
        var reversalQuantity = -_quantityChange;

        var reversal = Create(
            _variantId,
            ValueObjects.TransactionType.Reversal,
            reversalQuantity,
            currentStock,
            userId,
            notes ?? $"برگشت تراکنش {Id}",
            _referenceNumber);

        reversal._reversedByTransactionId = Id;
        MarkAsReversed();
        reversal.AddDomainEvent(new AdjustStockEvent(_variantId, reversal.StockAfter, reversalQuantity));
        return reversal;
    }

    public void MarkAsReversed()
    {
        if (_isReversed)
            throw new DomainException("این تراکنش قبلاً برگشت خورده است.");

        _isReversed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired() =>
        _expiresAt.HasValue && _expiresAt.Value < DateTime.UtcNow;

    #endregion Domain Behaviors

    #region Query Methods

    public bool IsIncrease() => _quantityChange > 0;

    public bool IsDecrease() => _quantityChange < 0;

    public bool IsReservation() => _transactionType == ValueObjects.TransactionType.Reservation.Value;

    public bool IsCommit() => _transactionType == ValueObjects.TransactionType.Commit.Value;

    public bool IsSale() => _transactionType == ValueObjects.TransactionType.Sale.Value;

    public bool IsReturn() => _transactionType == ValueObjects.TransactionType.Return.Value;

    public bool IsDamage() => _transactionType == ValueObjects.TransactionType.Damage.Value;

    public bool IsAdjustment() => _transactionType == ValueObjects.TransactionType.Adjustment.Value;

    public bool CanBeReversed()
    {
        if (_isReversed) return false;
        return _transactionType is var type &&
            (type == ValueObjects.TransactionType.Reservation.Value ||
             type == ValueObjects.TransactionType.StockIn.Value ||
             type == ValueObjects.TransactionType.StockOut.Value ||
             type == ValueObjects.TransactionType.Adjustment.Value);
    }

    public TransactionType GetTransactionTypeEnum()
    {
        return ValueObjects.TransactionType.FromString(_transactionType);
    }

    #endregion Query Methods

    #region Domain Invariants

    private void EnsureCanBeReversed()
    {
        if (_isReversed)
            throw new DomainException("این تراکنش قبلاً برگشت خورده است.");
        if (!CanBeReversed())
            throw new DomainException($"تراکنش از نوع '{_transactionType}' قابل برگشت نیست.");
    }

    private static void EnsureSufficientStock(int currentStock, int requestedQuantity)
    {
        if (currentStock < requestedQuantity)
            throw new DomainException(
                $"موجودی کافی نیست. موجودی فعلی: {currentStock}، درخواستی: {requestedQuantity}");
    }

    private static void ValidateNotes(string? notes)
    {
        if (!string.IsNullOrEmpty(notes) && notes.Length > MaxNotesLength)
            throw new DomainException($"یادداشت نمی‌تواند بیش از {MaxNotesLength} کاراکتر باشد.");
    }

    private static void ValidateReferenceNumber(string? referenceNumber)
    {
        if (!string.IsNullOrEmpty(referenceNumber) && referenceNumber.Length > MaxReferenceLength)
            throw new DomainException($"شماره مرجع نمی‌تواند بیش از {MaxReferenceLength} کاراکتر باشد.");
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (!string.IsNullOrEmpty(correlationId) && correlationId.Length > MaxCorrelationIdLength)
            throw new DomainException($"CorrelationId نمی‌تواند بیش از {MaxCorrelationIdLength} کاراکتر باشد.");
    }

    private void CheckLowStockWarning()
    {
        const int DefaultLowStockThreshold = 5;
        if (StockAfter <= 0)
            AddDomainEvent(new OutOfStockEvent(_variantId, 0, "محصول"));
        else if (StockAfter <= DefaultLowStockThreshold)
            AddDomainEvent(new LowStockWarningEvent(_variantId, 0, "محصول", StockAfter, DefaultLowStockThreshold));
    }

    #endregion Domain Invariants
}