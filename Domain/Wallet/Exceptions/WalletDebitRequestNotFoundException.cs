using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletDebitRequestNotFoundException(WalletDebitRequestId requestId)
    : DomainException($"درخواست کسر با شناسه {requestId.Value} یافت نشد.");
