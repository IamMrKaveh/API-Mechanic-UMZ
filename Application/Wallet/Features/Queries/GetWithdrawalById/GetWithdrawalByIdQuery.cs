using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWithdrawalById;

public sealed record GetWithdrawalByIdQuery(
    Guid WithdrawalId,
    Guid? RequesterUserId,
    bool IsAdmin) : IQuery<WalletWithdrawalRequestDto>;