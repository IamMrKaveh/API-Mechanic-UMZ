namespace Infrastructure.Payment.BackgroundServices;

public sealed class PaymentReconciliationService : BackgroundService
{
    private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan ReconciliationWindow = TimeSpan.FromHours(12);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentReconciliationService> _logger;

    public PaymentReconciliationService(
        IServiceProvider serviceProvider,
        ILogger<PaymentReconciliationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Reconciliation Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation.");
            }

            await Task.Delay(ReconciliationInterval, stoppingToken);
        }

        _logger.LogInformation("Payment Reconciliation Service stopped.");
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
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
                _logger.LogInformation(
                    "[Reconciliation] Checking stale pending transaction {TxId} for Order {OrderId}",
                    tx.Id, tx.OrderId);

                var gateway = gatewayFactory.GetGateway(tx.Gateway);
                var result = await gateway.VerifyPaymentAsync(
                    tx.Authority ?? string.Empty,
                    (int)tx.Amount.Amount);

                if (result.IsVerified)
                {
                    tx.MarkAsSuccess(result.RefId!.Value, result.CardPan);
                    reconciledCount++;

                    _logger.LogWarning(
                        "[Reconciliation] ⚠ Transaction {TxId} was actually PAID but system showed Pending. Fixed.",
                        tx.Id);
                }
                else
                {
                    tx.MarkAsFailed($"Reconciliation: {result.Message}");
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reconciliation] Failed to check transaction {TxId}", tx.Id);
            }
        }

        var verifiedTransactions = await dbContext.PaymentTransactions
            .Where(t => t.Status == "Verified" && t.CreatedAt >= DateTime.UtcNow.AddDays(-1))
            .ToListAsync(ct);

        var totalSystemAmount = verifiedTransactions.Sum(t => t.Amount.Amount);

        _logger.LogInformation(
            "[Reconciliation] Daily settlement summary: Verified={Verified}, TotalAmount={TotalAmount:N0} Toman, Reconciled={Reconciled}, Failed={Failed}",
            verifiedTransactions.Count, totalSystemAmount, reconciledCount, failedCount);

        if (reconciledCount > 0 || failedCount > 0)
            await unitOfWork.SaveChangesAsync(ct);
    }
}