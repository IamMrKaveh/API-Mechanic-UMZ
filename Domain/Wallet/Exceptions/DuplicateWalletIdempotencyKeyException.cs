namespace Domain.Wallet.Exceptions;

public sealed class DuplicateWalletIdempotencyKeyException : DomainException
{
    public string IdempotencyKey { get; }

    public DuplicateWalletIdempotencyKeyException(string idempotencyKey)
        : base($"عملیات کیف پول با کلید '{idempotencyKey}' قبلاً ثبت شده است.")
    {
        IdempotencyKey = idempotencyKey;
    }
}