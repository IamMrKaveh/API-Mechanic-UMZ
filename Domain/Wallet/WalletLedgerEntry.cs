namespace Domain.Wallet;

public class WalletLedgerEntry : BaseEntity
{
    private int _walletId;
    private int _userId;
    private decimal _amountDelta;
    private decimal _balanceAfter;
    private WalletTransactionType _transactionType;
    private WalletReferenceType _referenceType;
    private int _referenceId;
    private string _idempotencyKey = null!;
    private string? _correlationId;
    private string? _description;

    public int WalletId => _walletId;
    public int UserId => _userId;
    public decimal AmountDelta => _amountDelta;
    public decimal BalanceAfter => _balanceAfter;
    public WalletTransactionType TransactionType => _transactionType;
    public WalletReferenceType ReferenceType => _referenceType;
    public int ReferenceId => _referenceId;
    public string IdempotencyKey => _idempotencyKey;
    public string? CorrelationId => _correlationId;
    public string? Description => _description;
    public DateTime CreatedAt { get; private set; }

    private WalletLedgerEntry()
    { }

    internal static WalletLedgerEntry Create(
        int walletId,
        int userId,
        decimal amountDelta,
        decimal balanceAfter,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));

        return new WalletLedgerEntry
        {
            _walletId = walletId,
            _userId = userId,
            _amountDelta = amountDelta,
            _balanceAfter = balanceAfter,
            _transactionType = transactionType,
            _referenceType = referenceType,
            _referenceId = referenceId,
            _idempotencyKey = idempotencyKey,
            _correlationId = correlationId,
            _description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}