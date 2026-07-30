using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletDebitRequestStatusException(
    WalletDebitRequestId requestId,
    WalletDebitRequestStatus currentStatus)
    : DomainException($"وضعیت فعلی درخواست ({currentStatus}) اجازه این عملیات را نمی‌دهد.");
