namespace Domain.Wallet.Exceptions;

public sealed class DuplicateWalletIdempotencyKeyException(string idempotencyKey) : DomainException($"عملیات کیف پول با کلید '{idempotencyKey}' قبلاً ثبت شده است.")
{
    public string IdempotencyKey { get; } = idempotencyKey;
}