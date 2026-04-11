using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public record SyncCartPricesCommand(
    Guid? UserId,
    string GuestToken) : IRequest<ServiceResult<SyncCartPricesResult>>;