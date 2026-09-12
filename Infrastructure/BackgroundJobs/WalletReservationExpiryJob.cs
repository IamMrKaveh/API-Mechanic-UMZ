using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.Wallet.Enums;

namespace Infrastructure.BackgroundJobs;

public sealed class WalletReservationExpiryJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:wallet-reservation-expiry", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await ProcessExpiredReservationsAsync(ct);
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
                    .LogSystemEventAsync(
                        "WalletReservationExpiryError",
                        $"خطا در سرویس انقضای رزرو کیف پول: {ex.Message}",
                        ct);
            }

            await Task.Delay(CheckInterval, ct);
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var now = dateTimeProvider.UtcNow;

        var expiredReservations = await context.WalletReservations
            .AsNoTracking()
            .Where(r => r.Status == WalletReservationStatus.Active
                && r.ExpiresAt != null
                && r.ExpiresAt <= now)
            .Join(context.Wallets,
                r => r.WalletId,
                w => w.Id,
                (r, w) => new { Reservation = r, Wallet = w })
            .Where(x => x.Wallet.OwnerId != null)
            .OrderBy(x => x.Reservation.ExpiresAt)
            .Take(BatchSize)
            .Select(x => new { UserId = x.Wallet.OwnerId.Value, ReservationId = x.Reservation.Id.Value })
            .ToListAsync(ct);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                var command = new ReleaseWalletReservationCommand(
                    reservation.UserId,
                    reservation.ReservationId);

                await mediator.Send(command, ct);
            }
            catch (Exception ex)
            {
                await auditService.LogSystemEventAsync(
                    "WalletReservationExpiryItemError",
                    $"خطا در انقضای رزرو کیف پول: {ex.Message}",
                    ct);
            }
        }
    }
}