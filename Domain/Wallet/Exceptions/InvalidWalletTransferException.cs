namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletTransferException(string message) : DomainException(message)
{
    public override string ErrorCode => "WALLET_TRANSFER_INVALID";
}