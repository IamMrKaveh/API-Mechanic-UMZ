using SharedKernel.Abstractions.Interfaces;

namespace Tests.TestInfrastructure.Stubs;

/// <summary>
/// Shared fixed-clock <see cref="IDateTimeProvider"/> test double.
/// Replaces the copy-pasted <c>private sealed class FixedDateTimeProvider</c>
/// previously duplicated across validator test classes.
/// </summary>
public sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow.Kind == DateTimeKind.Utc
        ? utcNow
        : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
