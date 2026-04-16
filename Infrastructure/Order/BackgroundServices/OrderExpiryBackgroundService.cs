using Application.Order.Features.Commands.ExpireOrders;

namespace Infrastructure.Order.BackgroundServices;

/// <summary>
/// سرویس پس‌زمینه برای انقضای خودکار سفارش‌های پرداخت‌نشده.
/// هر دقیقه اجرا می‌شود و سفارش‌های قدیمی را Expire می‌کند.
/// </summary>
public sealed class OrderExpiryBackgroundService(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Order Expiry Service started.", ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunExpiryCheckAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await auditService.LogErrorAsync("Unhandled error in Order Expiry Service.", ct);
            }

            await Task.Delay(CheckInterval, ct);
        }

        await auditService.LogInformationAsync("Order Expiry Service stopped.", ct);
    }

    private async Task RunExpiryCheckAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ExpireOrdersCommand(), ct);

        if (result.IsSuccess)
        {
            await auditService.LogInformationAsync(
                "Order Expiry Service: {Count} orders expired. IDs: [{Ids}]",
                ct);
        }
    }
}