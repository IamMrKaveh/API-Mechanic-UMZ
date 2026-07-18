using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.PreviewWalletTransfer;

public sealed record PreviewWalletTransferQuery(
    string RecipientPhoneNumber,
    decimal Amount) : IQuery<WalletTransferPreviewDto>;