using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;

namespace Application.Wallet.Features.Queries.GetMyWalletDebitRequests;

public sealed record GetMyWalletDebitRequestsQuery(WalletDebitRequestStatus? Status)
    : IQuery<IReadOnlyList<WalletDebitRequestDto>>;
