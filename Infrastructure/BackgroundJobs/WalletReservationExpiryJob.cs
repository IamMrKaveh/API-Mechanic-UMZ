using Application.Audit.Contracts;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        var expiredReservations = await context.WalletLedgerEntries
            .AsNoTracking()
            .Join(context.Wallets,
                le => le.WalletId,
                w => w.Id,
                (le, w) => new { LedgerEntry = le, Wallet = w })
            .Where(x => x.Wallet.OwnerId != null)
            .Take(BatchSize)
            .Select(x => new
            {
                UserId = x.Wallet.OwnerId.Value
            })
            .ToListAsync(ct);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                var command = new ReleaseWalletReservationCommand(
                    reservation.UserId,
                    Guid.Empty);

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