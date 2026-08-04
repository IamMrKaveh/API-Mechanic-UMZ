namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletTransferException : DomainException
{
    public InvalidWalletTransferException(string message)
        : base("WALLET.TRANSFER.INVALID", message)
    {
    }

    public InvalidWalletTransferException(string errorCode, string message)
        : base(errorCode, message)
    {
    }

    public InvalidWalletTransferException(string errorCode, string message, IReadOnlyDictionary<string, object?> args)
        : base(errorCode, message, args)
    {
    }
}
