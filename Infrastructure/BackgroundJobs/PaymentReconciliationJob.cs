using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using SharedKernel.Exceptions;

namespace Infrastructure.BackgroundJobs;

public sealed class PaymentReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan ReconciliationWindow = TimeSpan.FromHours(12);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromHours(1);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startScope = scopeFactory.CreateScope())
        {
            await startScope.ServiceProvider.GetRequiredService<IAuditService>()
                .LogInformationAsync("Payment Reconciliation Service started.", ct);
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:payment-reconciliation", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await RunReconciliationAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogErrorAsync($"Error during payment reconciliation: {ex.Message}", ct);
            }

            await Task.Delay(ReconciliationInterval, ct);
        }

        using var stopScope = scopeFactory.CreateScope();
        await stopScope.ServiceProvider.GetRequiredService<IAuditService>()
            .LogInformationAsync("Payment Reconciliation Service stopped.", ct);
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
        var gatewayFactory = scope.ServiceProvider.GetRequiredService<IPaymentGatewayFactory>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var threshold = dateTimeProvider.UtcNow.Subtract(ReconciliationWindow);
        var totalReconciled = 0;
        var totalFailed = 0;

        while (true)
        {
            var batch = await dbContext.PaymentTransactions
                .Where(t =>
                    t.Status == Domain.Payment.ValueObjects.PaymentStatus.Pending &&
                    t.CreatedAt <= threshold)
                .OrderBy(t => t.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            var batchReconciled = 0;
            var batchFailed = 0;

            foreach (var tx in batch)
            {
                try
                {
                    var gateway = gatewayFactory.GetGateway(tx.Gateway.Value);

                    PaymentVerificationResult verifyValue;
                    try
                    {
                        verifyValue = await gateway.VerifyAsync(tx.Authority.Value, tx.Amount, ct);
                    }
                    catch (ExternalServiceException ex)
                    {
                        tx.MarkAsFailed(dateTimeProvider.UtcNow, $"Reconciliation: {ex.Message}");
                        batchFailed++;
                        continue;
                    }

                    if (verifyValue.IsVerified && verifyValue.RefId.HasValue && verifyValue.RefId.Value > 0)
                    {
                        tx.MarkAsSuccess(verifyValue.RefId.Value, dateTimeProvider.UtcNow, verifyValue.Fee);
                        batchReconciled++;

                        await auditService.LogWarningAsync(
                            $"[Reconciliation] Transaction {tx.Id.Value} was PAID but showed Pending. Fixed.", ct);
                    }
                    else
                    {
                        tx.MarkAsFailed(dateTimeProvider.UtcNow, "Reconciliation: پرداخت تأیید نشد.");
                        batchFailed++;
                    }
                }
                catch (Exception ex)
                {
                    await auditService.LogErrorAsync(
                        $"[Reconciliation] Failed to check transaction {tx.Id.Value}: {ex.Message}", ct);
                }
            }

            if (batchReconciled > 0 || batchFailed > 0)
                await unitOfWork.SaveChangesAsync(ct);

            totalReconciled += batchReconciled;
            totalFailed += batchFailed;
        }

        await auditService.LogInformationAsync(
            $"[Reconciliation] Complete. Reconciled={totalReconciled}, Failed={totalFailed}", ct);
    }
}