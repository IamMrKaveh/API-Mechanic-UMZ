namespace Domain.Wallet;

public class Wallet : AggregateRoot, IAuditable
{
    private readonly List<WalletLedgerEntry> _ledgerEntries = new();
    private readonly List<WalletReservation> _reservations = new();

    private int _userId;
    private decimal _currentBalance;
    private decimal _reservedBalance;
    private WalletStatus _status;

    public int UserId => _userId;
    public decimal CurrentBalance => _currentBalance;
    public decimal ReservedBalance => _reservedBalance;
    public decimal AvailableBalance => _currentBalance - _reservedBalance;
    public WalletStatus Status => _status;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<WalletLedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();
    public IReadOnlyCollection<WalletReservation> Reservations => _reservations.AsReadOnly();

    private Wallet()
    { }

    #region Factory Methods

    public static Wallet Create(int userId)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        var wallet = new Wallet
        {
            _userId = userId,
            _currentBalance = 0,
            _reservedBalance = 0,
            _status = WalletStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        return wallet;
    }

    #endregion Factory Methods

    #region Lifecycle

    public void Suspend(string reason)
    {
        if (_status == WalletStatus.Suspended) return;
        if (_status == WalletStatus.Closed)
            throw new DomainException("کیف پول بسته شده و قابل تغییر نیست.");

        _status = WalletStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WalletStatusChangedEvent(Id, _userId, WalletStatus.Suspended, reason));
    }

    public void Reactivate()
    {
        if (_status == WalletStatus.Active) return;
        if (_status == WalletStatus.Closed)
            throw new DomainException("کیف پول بسته شده و قابل بازگردانی نیست.");

        _status = WalletStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WalletStatusChangedEvent(Id, _userId, WalletStatus.Active, null));
    }

    public void Close()
    {
        if (_status == WalletStatus.Closed) return;
        if (_currentBalance != 0 || _reservedBalance != 0)
            throw new DomainException("کیف پول دارای موجودی است و قابل بستن نیست.");

        _status = WalletStatus.Closed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WalletStatusChangedEvent(Id, _userId, WalletStatus.Closed, null));
    }

    private void EnsureActive()
    {
        if (_status == WalletStatus.Suspended)
            throw new DomainException("کیف پول معلق است و عملیات مجاز نمی‌باشد.");
        if (_status == WalletStatus.Closed)
            throw new DomainException("کیف پول بسته شده است.");
    }

    #endregion Lifecycle

    #region Credit / Debit

    public WalletLedgerEntry Credit(
        Money amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null)
    {
        EnsureActive();

        Guard.Against.Null(amount, nameof(amount));
        if (amount.Amount <= 0)
            throw new DomainException("مبلغ شارژ باید بزرگتر از صفر باشد.");

        if (_ledgerEntries.Any(e => e.IdempotencyKey == idempotencyKey))
            throw new Exceptions.DuplicateWalletIdempotencyKeyException(idempotencyKey);

        _currentBalance += amount.Amount;
        UpdatedAt = DateTime.UtcNow;

        var entry = WalletLedgerEntry.Create(
            Id,
            _userId,
            amount.Amount,
            _currentBalance,
            transactionType,
            referenceType,
            referenceId,
            idempotencyKey,
            correlationId,
            description);

        _ledgerEntries.Add(entry);

        AddDomainEvent(new Events.WalletCreditedEvent(Id, _userId, amount.Amount, referenceType, referenceId));
        return entry;
    }

    public WalletLedgerEntry Debit(
        Money amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null)
    {
        EnsureActive();

        Guard.Against.Null(amount, nameof(amount));
        if (amount.Amount <= 0)
            throw new DomainException("مبلغ برداشت باید بزرگتر از صفر باشد.");

        if (_ledgerEntries.Any(e => e.IdempotencyKey == idempotencyKey))
            throw new Exceptions.DuplicateWalletIdempotencyKeyException(idempotencyKey);

        if (AvailableBalance < amount.Amount)
            throw new Exceptions.InsufficientWalletBalanceException(_userId, AvailableBalance, amount.Amount);

        _currentBalance -= amount.Amount;
        UpdatedAt = DateTime.UtcNow;

        var entry = WalletLedgerEntry.Create(
            Id,
            _userId,
            -amount.Amount,
            _currentBalance,
            transactionType,
            referenceType,
            referenceId,
            idempotencyKey,
            correlationId,
            description);

        _ledgerEntries.Add(entry);

        AddDomainEvent(new Events.WalletDebitedEvent(Id, _userId, amount.Amount, referenceType, referenceId));
        return entry;
    }

    #endregion Credit / Debit

    #region Reservation

    public WalletReservation Reserve(
        Money amount,
        int orderId,
        DateTime? expiresAt = null)
    {
        EnsureActive();

        Guard.Against.Null(amount, nameof(amount));
        if (amount.Amount <= 0)
            throw new DomainException("مبلغ رزرو باید بزرگتر از صفر باشد.");

        if (AvailableBalance < amount.Amount)
            throw new Exceptions.InsufficientWalletBalanceException(_userId, AvailableBalance, amount.Amount);

        _reservedBalance += amount.Amount;
        UpdatedAt = DateTime.UtcNow;

        var reservation = WalletReservation.Create(Id, amount.Amount, orderId, expiresAt);
        _reservations.Add(reservation);

        AddDomainEvent(new Events.WalletReservationCreatedEvent(Id, _userId, amount.Amount, orderId));
        return reservation;
    }

    public void CommitReservation(int orderId, string idempotencyKey)
    {
        var reservation = _reservations.FirstOrDefault(r => r.OrderId == orderId && r.Status == WalletReservationStatus.Pending);
        if (reservation == null)
            throw new DomainException($"رزرو فعالی برای سفارش {orderId} یافت نشد.");

        reservation.Commit();
        _reservedBalance -= reservation.Amount;
        _currentBalance -= reservation.Amount;
        UpdatedAt = DateTime.UtcNow;

        var entry = WalletLedgerEntry.Create(
            Id,
            _userId,
            -reservation.Amount,
            _currentBalance,
            WalletTransactionType.OrderPayment,
            WalletReferenceType.Order,
            orderId,
            idempotencyKey,
            null,
            "تسویه رزرو سفارش");

        _ledgerEntries.Add(entry);

        AddDomainEvent(new Events.WalletReservationCommittedEvent(Id, _userId, reservation.Amount, orderId));
    }

    public void ReleaseReservation(int orderId)
    {
        var reservation = _reservations.FirstOrDefault(r => r.OrderId == orderId && r.Status == WalletReservationStatus.Pending);
        if (reservation == null)
            return;

        reservation.Release();
        _reservedBalance -= reservation.Amount;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new Events.WalletReservationReleasedEvent(Id, _userId, reservation.Amount, orderId));
    }

    public void ExpireReservations()
    {
        var expiredReservations = _reservations
            .Where(r => r.IsExpired())
            .ToList();

        foreach (var reservation in expiredReservations)
        {
            reservation.Expire();
            _reservedBalance -= reservation.Amount;
        }

        if (expiredReservations.Any())
            UpdatedAt = DateTime.UtcNow;
    }

    #endregion Reservation

    #region Query Methods

    public bool HasSufficientBalance(decimal amount) => AvailableBalance >= amount;

    public bool HasIdempotencyKey(string key) => _ledgerEntries.Any(e => e.IdempotencyKey == key);

    public bool IsActive => _status == WalletStatus.Active;

    #endregion Query Methods
}