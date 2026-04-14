using Application.Wallet.Features.Commands.ReleaseWalletReservation;

namespace Infrastructure.BackgroundJobs;

public sealed class WalletReservationExpiryJob(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await auditService.LogSystemEventAsync(
                    "WalletReservationExpiryError",
                    $"خطا در سرویس انقضای رزرو کیف پول: {ex.Message}",
                    ct);
            }

            await Task.Delay(CheckInterval, ct);
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();

        var now = DateTime.UtcNow;
        var expiredReservations = await context.WalletReservations
            .AsNoTracking()
            .Where(r => r.Status == Domain.Wallet.Enums.WalletReservationStatus.Pending
                        && r.ExpiresAt.HasValue && r.ExpiresAt.Value <= now)
            .Take(BatchSize)
            .Select(r => new
            {
                UserId = r.Wallet.OwnerId.Value,
                OrderId = r.OrderId.Value
            })
            .ToListAsync(ct);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                var command = new ReleaseWalletReservationCommand(
                    reservation.UserId,
                    reservation.OrderId);

                await mediator.Send(command, ct);
            }
            catch (Exception ex)
            {
                await auditService.LogSystemEventAsync(
                    "WalletReservationExpiryItemError",
                    $"خطا در انقضای رزرو کیف پول برای سفارش {reservation.OrderId}: {ex.Message}",
                    ct);
            }
        }
    }
}