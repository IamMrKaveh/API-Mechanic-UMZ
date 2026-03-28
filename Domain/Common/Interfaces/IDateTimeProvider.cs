namespace Domain.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow => DateTime.UtcNow;
}