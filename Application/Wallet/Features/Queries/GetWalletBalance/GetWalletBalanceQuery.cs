using Application.Common.Models;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(
    int UserId
    ) : IRequest<ServiceResult<WalletDto>>;