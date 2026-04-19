using Application.Payment.Contracts;

namespace Infrastructure.BackgroundJobs;

public sealed class PaymentReconciliationJob(
    DBContext dbContext,
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan ReconciliationWindow = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Payment Reconciliation Service started.", ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync($"Error during payment reconciliation: {ex.Message}", ct);
            }

            await Task.Delay(ReconciliationInterval, ct);
        }

        await auditService.LogInformationAsync("Payment Reconciliation Service stopped.", ct);
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var gatewayFactory = scope.ServiceProvider.GetRequiredService<IPaymentGatewayFactory>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var threshold = DateTime.UtcNow.Subtract(ReconciliationWindow);

        var stalePendingTransactions = await dbContext.PaymentTransactions
            .Where(t => t.Status == Domain.Payment.ValueObjects.PaymentStatus.Pending && t.CreatedAt <= threshold)
            .ToListAsync(ct);

        var reconciledCount = 0;
        var failedCount = 0;

        foreach (var tx in stalePendingTransactions)
        {
            try
            {
                var gateway = gatewayFactory.GetGateway(tx.Gateway.Value);
                var verifyResult = await gateway.VerifyAsync(
                    tx.Authority.Value,
                    tx.Amount,
                    ct);

                if (verifyResult.IsSuccess && verifyResult.Value.IsVerified)
                {
                    var now = DateTime.UtcNow;
                    tx.MarkAsSuccess(verifyResult.Value.RefId!.Value, now, verifyResult.Value.Fee);
                    reconciledCount++;

                    await auditService.LogWarningAsync(
                        $"[Reconciliation] Transaction {tx.Id.Value} was PAID but showed Pending. Fixed.", ct);
                }
                else
                {
                    tx.MarkAsFailed(DateTime.UtcNow, "Reconciliation: پرداخت تأیید نشد.");
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"[Reconciliation] Failed to check transaction {tx.Id.Value}: {ex.Message}", ct);
            }
        }

        await auditService.LogInformationAsync(
            $"[Reconciliation] Complete. Reconciled={reconciledCount}, Failed={failedCount}", ct);

        if (reconciledCount > 0 || failedCount > 0)
            await unitOfWork.SaveChangesAsync(ct);
    }
}