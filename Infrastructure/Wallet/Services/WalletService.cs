using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.DebitWallet;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Application.Wallet.Features.Commands.ReserveWallet;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.Services;

public sealed class WalletService(IMediator mediator) : IWalletService
{
    private readonly IMediator _mediator = mediator;

    public async Task<ServiceResult<WalletDto>> GetBalanceAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        return await _mediator.Send(new GetWalletBalanceQuery(userId.Value), ct);
    }

    public async Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> GetLedgerAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _mediator.Send(new GetWalletLedgerQuery(userId.Value, page, pageSize), ct);
    }

    public async Task<ServiceResult<Unit>> CreditAsync(
        UserId userId,
        Money amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        string referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new CreditWalletCommand(
                userId.Value,
                amount.Amount,
                transactionType,
                referenceType,
                referenceId,
                idempotencyKey,
                correlationId,
                description),
            ct);
    }

    public async Task<ServiceResult<Unit>> DebitAsync(
        UserId userId,
        Money amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        string referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new DebitWalletCommand(
                userId.Value,
                amount.Amount,
                transactionType,
                referenceType,
                referenceId,
                idempotencyKey,
                correlationId,
                description),
            ct);
    }

    public async Task<ServiceResult<Unit>> ReserveAsync(
        UserId userId,
        decimal amount,
        OrderId orderId,
        DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new ReserveWalletCommand(userId.Value, amount, orderId.Value, expiresAt), ct);
    }

    public async Task<ServiceResult<Unit>> ReleaseReservationAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await _mediator.Send(
            new ReleaseWalletReservationCommand(userId.Value, orderId.Value), ct);
    }

    public Task<ServiceResult<Unit>> ReserveAsync(UserId userId, Money amount, OrderId orderId, DateTime? expiresAt = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}