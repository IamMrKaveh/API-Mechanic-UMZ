namespace Domain.Wallet.Entities;

public sealed class WalletReservation : Entity<WalletReservationId>
{
    private WalletReservation()
    {
    }

    public WalletId WalletId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public string Purpose { get; private set; } = default!;
    public WalletReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public bool IsActive => Status == WalletReservationStatus.Active;

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value && Status == WalletReservationStatus.Active;

    internal static WalletReservation Create(
        WalletReservationId id,
        WalletId walletId,
        Money amount,
        string purpose,
        DateTime? expiresAt = null)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(walletId, nameof(walletId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.NullOrWhiteSpace(purpose, nameof(purpose));

        return new WalletReservation
        {
            Id = id,
            WalletId = walletId,
            Amount = amount,
            Purpose = purpose,
            Status = WalletReservationStatus.Active,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal void Confirm()
    {
        if (Status != WalletReservationStatus.Active)
            throw new DomainException($"رزرو در وضعیت '{Status}' قابل تأیید نیست.");

        Status = WalletReservationStatus.Confirmed;
        ResolvedAt = DateTime.UtcNow;
    }

    internal void Release()
    {
        if (Status != WalletReservationStatus.Active)
            throw new DomainException($"رزرو در وضعیت '{Status}' قابل آزادسازی نیست.");

        Status = WalletReservationStatus.Released;
        ResolvedAt = DateTime.UtcNow;
    }

    internal void MarkExpired()
    {
        if (Status != WalletReservationStatus.Active)
            return;

        Status = WalletReservationStatus.Released;
        ResolvedAt = DateTime.UtcNow;
    }
}