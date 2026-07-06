using System.Diagnostics;
using Domain.User.Events;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class UserCreatedEventHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<UserCreatedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRegisteredEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(UserRegisteredEvent)
        }))
        {
            try
            {
                var walletId = WalletId.NewId();
                var userId = domainEvent.UserId;
                var wallet = Domain.Wallet.Aggregates.Wallet.Create(walletId, userId, "IRR");

                await walletRepository.AddAsync(wallet, ct);
                await unitOfWork.SaveChangesAsync(ct);

                logger.LogInformation(
                    "Wallet {WalletId} created for user {UserId}",
                    walletId.Value,
                    userId.Value);

                await auditService.LogSystemEventAsync(
                    "Wallet creation",
                    $"Wallet created for user {userId.Value}",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to create wallet for user {UserId}",
                    domainEvent.UserId);

                await auditService.LogSystemEventAsync(
                    ex.Message,
                    $"Failed to create wallet for user {domainEvent.UserId}",
                    ct);
            }
        }
    }
}