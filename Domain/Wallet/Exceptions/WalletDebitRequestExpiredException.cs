using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletDebitRequestExpiredException(WalletDebitRequestId requestId)
    : DomainException("مهلت پاسخ به این درخواست به پایان رسیده است.");
