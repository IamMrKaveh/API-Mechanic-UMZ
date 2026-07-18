using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWithdrawalById;

public sealed record GetWithdrawalByIdQuery(Guid Id)
    : IQuery<WalletWithdrawalRequestDto>;