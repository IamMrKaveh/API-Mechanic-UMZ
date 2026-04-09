using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public record SyncCartPricesCommand : IRequest<ServiceResult<SyncCartPricesResult>>;