namespace Domain.Wallet;

public enum WalletReservationStatus
{
    Pending,
    Committed,
    Released,
    Expired
}

public class WalletReservation : BaseEntity
{
    private int _walletId;
    private decimal _amount;
    private int _orderId;
    private WalletReservationStatus _status;
    private DateTime? _expiresAt;

    public int WalletId => _walletId;
    public decimal Amount => _amount;
    public int OrderId => _orderId;
    public WalletReservationStatus Status => _status;
    public DateTime? ExpiresAt => _expiresAt;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private WalletReservation()
    { }

    internal static WalletReservation Create(int walletId, decimal amount, int orderId, DateTime? expiresAt = null)
    {
        return new WalletReservation
        {
            _walletId = walletId,
            _amount = amount,
            _orderId = orderId,
            _status = WalletReservationStatus.Pending,
            _expiresAt = expiresAt ?? DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };
    }

    internal void Commit()
    {
        if (_status != WalletReservationStatus.Pending)
            throw new DomainException("فقط رزروهای در انتظار قابل تسویه هستند.");

        _status = WalletReservationStatus.Committed;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Release()
    {
        if (_status != WalletReservationStatus.Pending)
            return;

        _status = WalletReservationStatus.Released;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Expire()
    {
        if (_status != WalletReservationStatus.Pending)
            return;

        _status = WalletReservationStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired() => _status == WalletReservationStatus.Pending
        && _expiresAt.HasValue
        && _expiresAt.Value < DateTime.UtcNow;
}