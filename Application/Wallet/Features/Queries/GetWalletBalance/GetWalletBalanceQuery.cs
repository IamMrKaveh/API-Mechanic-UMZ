using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(Guid UserId) : IRequest<ServiceResult<WalletDto>>;