namespace Application.Cart.Features.Commands.SyncCartPrices;

public record SyncCartPricesCommand(
    Guid? UserId,
    string GuestToken) : ICommand;