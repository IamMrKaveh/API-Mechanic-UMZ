using Application.Common.Events;
using Domain.User.Events;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Auth.EventHandlers;

public sealed class UserCreatedEventHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRegisteredEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        try
        {
            var walletId = WalletId.NewId();
            var userId = domainEvent.UserId;
            var wallet = Domain.Wallet.Aggregates.Wallet.Create(walletId, userId, "IRR");

            await walletRepository.AddAsync(wallet, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "Wallet creation",
                $"Wallet created for user {userId.Value}",
                ct);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                ex.Message,
                $"Failed to create wallet for user {domainEvent.UserId}",
                ct);
        }
    }
}