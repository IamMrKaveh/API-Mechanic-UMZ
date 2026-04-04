using Application.Common.Results;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(int UserId) : IRequest<ServiceResult<WalletDto>>;