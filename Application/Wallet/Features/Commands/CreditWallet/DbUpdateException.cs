namespace Application.Wallet.Features.Commands.CreditWallet;

[Serializable]
internal class DbUpdateException : Exception
{
    public DbUpdateException()
    {
    }

    public DbUpdateException(string? message) : base(message)
    {
    }

    public DbUpdateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}