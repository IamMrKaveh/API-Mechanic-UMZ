using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletTransferById;

public sealed record GetWalletTransferByIdQuery(Guid Id) : IQuery<WalletTransferDto>;
