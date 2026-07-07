namespace Application.Common.Exceptions;

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