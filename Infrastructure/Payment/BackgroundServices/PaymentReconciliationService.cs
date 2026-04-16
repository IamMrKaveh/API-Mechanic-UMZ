using Application.Payment.Contracts;

namespace Infrastructure.Payment.BackgroundServices;

public sealed class PaymentReconciliationService(
    DBContext dbContext,
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan ReconciliationWindow = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Payment Reconciliation Service started.");

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
                _logger.LogError(ex, "Error during payment reconciliation.");
            }

            await Task.Delay(ReconciliationInterval, ct);
        }

        _logger.LogInformation("Payment Reconciliation Service stopped.");
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();

        var gatewayFactory = scope.ServiceProvider.GetRequiredService<IPaymentGatewayFactory>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var threshold = DateTime.UtcNow.Subtract(ReconciliationWindow);

        var stalePendingTransactions = await dbContext.PaymentTransactions
            .Where(t => t.Status == "Pending" && t.CreatedAt <= threshold)
            .ToListAsync(ct);

        var reconciledCount = 0;
        var failedCount = 0;

        foreach (var tx in stalePendingTransactions)
        {
            try
            {
                await auditService.LogInformationAsync(
                    "[Reconciliation] Checking stale pending transaction {TxId} for Order {OrderId}",
                    ct);

                var gateway = gatewayFactory.GetGateway(tx.Gateway);
                var result = await gateway.VerifyPaymentAsync(
                    tx.Authority ?? string.Empty,
                    (int)tx.Amount.Amount);

                if (result.IsVerified)
                {
                    tx.MarkAsSuccess(result.RefId!.Value, result.CardPan);
                    reconciledCount++;

                    await auditService.LogWarningAsync(
                        "[Reconciliation] ⚠ Transaction {TxId} was actually PAID but system showed Pending. Fixed.",
                        ct);
                }
                else
                {
                    tx.MarkAsFailed($"Reconciliation: {result.Message}");
                    failedCount++;
                }
            }
            catch (Exception)
            {
                await auditService.LogErrorAsync("[Reconciliation] Failed to check transaction {TxId}", ct);
            }
        }

        var verifiedTransactions = await dbContext.PaymentTransactions
            .Where(t => t.Status == "Verified" && t.CreatedAt >= DateTime.UtcNow.AddDays(-1))
            .ToListAsync(ct);

        var totalSystemAmount = verifiedTransactions.Sum(t => t.Amount.Amount);

        await auditService.LogInformationAsync(
            "[Reconciliation] Daily settlement summary: Verified={Verified}, TotalAmount={TotalAmount:N0} Toman, Reconciled={Reconciled}, Failed={Failed}",
            ct);

        if (reconciledCount > 0 || failedCount > 0)
            await unitOfWork.SaveChangesAsync(ct);
    }
}