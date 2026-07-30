using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class UnauthorizedWalletDebitApprovalException(WalletDebitRequestId requestId)
    : DomainException("فقط صاحب کیف پول می‌تواند این درخواست را تایید یا رد کند.");
