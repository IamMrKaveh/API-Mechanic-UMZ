using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetPendingDebitRequestsByUser;

public sealed record GetPendingDebitRequestsByUserQuery(Guid UserId)
    : IQuery<IReadOnlyList<WalletDebitRequestDto>>;
