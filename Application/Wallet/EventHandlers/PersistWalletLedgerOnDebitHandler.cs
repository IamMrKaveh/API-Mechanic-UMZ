using Domain.Wallet.Entities;
using Domain.Wallet.Events;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.EventHandlers;

public sealed class PersistWalletLedgerOnDebitHandler(
    IWalletLedgerRepository ledgerRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletDebitedEvent>>
{
    public async Task Handle(
        DomainEventNotification<WalletDebitedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        if (!string.IsNullOrWhiteSpace(evt.IdempotencyKey))
        {
            var duplicate = await ledgerRepository.HasIdempotencyKeyAsync(
                evt.OwnerId, evt.IdempotencyKey, ct);
            if (duplicate)
                return;
        }

        try
        {
            var entry = WalletLedgerEntry.FromDebitEvent(evt);
            await ledgerRepository.AddAsync(entry, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await auditService.LogInformationAsync(
                $"WalletLedger debit already persisted (idempotency hit). WalletId={evt.WalletId.Value}, IdempotencyKey={evt.IdempotencyKey}",
                ct);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Failed to persist wallet debit ledger. WalletId={evt.WalletId.Value}, IdempotencyKey={evt.IdempotencyKey}, Error={ex.Message}",
                ct);
            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;

        var msg = inner.Message ?? string.Empty;
        return msg.Contains("IX_WalletLedgerEntries_IdempotencyKey", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
