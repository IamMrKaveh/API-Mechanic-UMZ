namespace Infrastructure.BackgroundJobs.Options;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 30;
}