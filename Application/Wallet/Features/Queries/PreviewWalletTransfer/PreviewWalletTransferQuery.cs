using Application.Common.Interfaces;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.PreviewWalletTransfer;

public sealed record PreviewWalletTransferQuery(
    Guid FromUserId,
    string RecipientPhoneNumber,
    decimal Amount) : IQuery<WalletTransferPreviewDto>;