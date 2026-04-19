namespace SharedKernel.Abstractions.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}