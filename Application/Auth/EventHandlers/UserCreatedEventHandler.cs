using Domain.User.Events;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Auth.EventHandlers;

public sealed class UserCreatedEventHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<UserCreatedEventHandler> logger) : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(
        UserRegisteredEvent notification,
        CancellationToken ct)
    {
        try
        {
            var walletId = WalletId.NewId();
            var userId = notification.UserId;
            var wallet = Wallet.Create(walletId, userId, "IRR");

            await walletRepository.AddAsync(wallet, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Wallet created for user {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create wallet for user {UserId}", notification.UserId.Value);
        }
    }
}