namespace Infrastructure.Wallet.Services;

public sealed class WalletService : IWalletService
{
    private readonly IMediator _mediator;
    private readonly ILogger<WalletService> _logger;

    public WalletService(IMediator mediator, ILogger<WalletService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ServiceResult<WalletDto>> GetBalanceAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new GetWalletBalanceQuery(userId), ct);
    }

    public async Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> GetLedgerAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new GetWalletLedgerQuery(userId, page, pageSize), ct);
    }

    public async Task<ServiceResult<Unit>> CreditAsync(
        int userId,
        decimal amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new CreditWalletCommand(
                userId,
                amount,
                transactionType,
                referenceType,
                referenceId,
                idempotencyKey,
                correlationId,
                description),
            ct);
    }

    public async Task<ServiceResult<Unit>> DebitAsync(
        int userId,
        decimal amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new DebitWalletCommand(
                userId,
                amount,
                transactionType,
                referenceType,
                referenceId,
                idempotencyKey,
                correlationId,
                description),
            ct);
    }

    public async Task<ServiceResult<Unit>> ReserveAsync(
        int userId,
        decimal amount,
        int orderId,
        DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new ReserveWalletCommand(userId, amount, orderId, expiresAt), ct);
    }

    public async Task<ServiceResult<Unit>> ReleaseReservationAsync(
        int userId,
        int orderId,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new ReleaseWalletReservationCommand(userId, orderId), ct);
    }
}