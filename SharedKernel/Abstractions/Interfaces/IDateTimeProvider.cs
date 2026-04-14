namespace SharedKernel.Abstractions.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow => DateTime.UtcNow;
    DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}