namespace Infrastructure.BackgroundJobs.Options;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";
    public int ExpiryMinutes { get; init; } = 30;
}