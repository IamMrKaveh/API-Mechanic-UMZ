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

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Navigation - فقط خواندنی برای EF Core
    public ProductVariant? Variant { get; private set; }

    public OrderItem? OrderItem { get; private set; }
    public User.User? User { get; private set; }
    public ICollection<InventoryTransaction>? ReversalTransactions { get; private set; }

    // Computed - Domain Logic
    public int StockAfter => _stockBefore + _quantityChange;

    // Business Constants
    private const int MaxNotesLength = 500;

    private const int MaxReferenceLength = 100;

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
        int? orderItemId = null)
    {
        Guard.Against.NegativeOrZero(variantId, nameof(variantId));
        ValidateNotes(notes);
        ValidateReferenceNumber(referenceNumber);

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
        string? referenceNumber = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.StockIn,
            quantity,
            stockBefore,
            userId,
            notes ?? "افزایش موجودی",
            referenceNumber);

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
        string? referenceNumber = null)
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
            orderItemId);

        transaction.AddDomainEvent(new StockReservedEvent(variantId, 0, quantity));

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
        {
            throw new NegativeStockException(variantId, stockBefore, Math.Abs(quantityChange));
        }

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
        string? notes = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var transaction = Create(
            variantId,
            ValueObjects.TransactionType.Return,
            quantity,
            stockBefore,
            userId,
            notes ?? "برگشت از فروش",
            orderItemId: orderItemId);

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
        {
            throw new DomainException("این تراکنش قبلاً برگشت خورده است.");
        }

        _isReversed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Behaviors

    #region Query Methods

    public bool IsIncrease() => _quantityChange > 0;

    public bool IsDecrease() => _quantityChange < 0;

    public bool IsReservation() => _transactionType == ValueObjects.TransactionType.Reservation.Value;

    public bool IsSale() => _transactionType == ValueObjects.TransactionType.Sale.Value;

    public bool IsReturn() => _transactionType == ValueObjects.TransactionType.Return.Value;

    public bool IsDamage() => _transactionType == ValueObjects.TransactionType.Damage.Value;

    public bool IsAdjustment() => _transactionType == ValueObjects.TransactionType.Adjustment.Value;

    public bool CanBeReversed()
    {
        if (_isReversed)
            return false;

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
        {
            throw new DomainException("این تراکنش قبلاً برگشت خورده است.");
        }

        if (!CanBeReversed())
        {
            throw new DomainException($"تراکنش از نوع '{_transactionType}' قابل برگشت نیست.");
        }
    }

    private static void EnsureSufficientStock(int currentStock, int requestedQuantity)
    {
        if (currentStock < requestedQuantity)
        {
            throw new DomainException($"موجودی کافی نیست. موجودی فعلی: {currentStock}، درخواستی: {requestedQuantity}");
        }
    }

    private static void ValidateNotes(string? notes)
    {
        if (!string.IsNullOrEmpty(notes) && notes.Length > MaxNotesLength)
        {
            throw new DomainException($"یادداشت نمی‌تواند بیش از {MaxNotesLength} کاراکتر باشد.");
        }
    }

    private static void ValidateReferenceNumber(string? referenceNumber)
    {
        if (!string.IsNullOrEmpty(referenceNumber) && referenceNumber.Length > MaxReferenceLength)
        {
            throw new DomainException($"شماره مرجع نمی‌تواند بیش از {MaxReferenceLength} کاراکتر باشد.");
        }
    }

    private void CheckLowStockWarning()
    {
        const int DefaultLowStockThreshold = 5;

        if (StockAfter <= 0)
        {
            AddDomainEvent(new OutOfStockEvent(_variantId, 0, "محصول"));
        }
        else if (StockAfter <= DefaultLowStockThreshold)
        {
            AddDomainEvent(new LowStockWarningEvent(_variantId, 0, "محصول", StockAfter, DefaultLowStockThreshold));
        }
    }

    #endregion Domain Invariants
}