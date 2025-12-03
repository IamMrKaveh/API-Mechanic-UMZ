namespace Infrastructure.BackgroundJobs;

public class PaymentVerificationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentVerificationJob> _logger;

    public PaymentVerificationJob(IServiceProvider serviceProvider, ILogger<PaymentVerificationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Verification Job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LedkaContext>();
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var pendingTransactions = await context.PaymentTransactions
                    .Where(t => (t.Status == PaymentTransaction.PaymentStatuses.VerificationInProgress || t.Status == PaymentTransaction.PaymentStatuses.Pending)
                                && t.Authority != null
                                && t.CreatedAt < cutoff
                                && t.Status != PaymentTransaction.PaymentStatuses.Success
                                && t.Status != PaymentTransaction.PaymentStatuses.Failed
                                && t.Status != PaymentTransaction.PaymentStatuses.Expired)
                    .OrderBy(t => t.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var tx in pendingTransactions)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        _logger.LogInformation("Retrying verification for transaction {Id}, Authority {Authority}", tx.Id, tx.Authority);
                        await paymentService.VerifyPaymentAsync(tx.Authority, "OK"); // Assume OK to trigger check
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error re-verifying transaction {Id}", tx.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Payment Verification Job");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}