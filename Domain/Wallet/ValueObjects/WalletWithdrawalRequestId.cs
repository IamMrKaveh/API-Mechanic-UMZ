namespace Domain.Wallet.ValueObjects;

public sealed record WalletWithdrawalRequestId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletWithdrawalRequestId(Guid value) => Value = value;

    public static WalletWithdrawalRequestId NewId() => new(Guid.NewGuid());

    public static WalletWithdrawalRequestId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletWithdrawalRequestId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WalletWithdrawalRequestId id) => id.Value;
}