using Application.Analytics.Features.Queries.GetDashboardStatistics;
using FluentValidation.TestHelper;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Analytics.Features.Queries.GetDashboardStatistics;

public class GetDashboardStatisticsValidatorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 08, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly GetDashboardStatisticsValidator _sut =
        new(new FixedDateTimeProvider(FixedUtcNow));

    [Fact]
    public void BothDatesNull_ShouldNotHaveError()
    {
        var query = new GetDashboardStatisticsQuery(null, null);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void FromDateAfterToDate_ShouldHaveError()
    {
        var from = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetDashboardStatisticsQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void FromDateInFuture_ShouldHaveError()
    {
        var from = FixedUtcNow.AddDays(1);
        var query = new GetDashboardStatisticsQuery(from, null);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع نمی‌تواند در آینده باشد.");
    }

    [Fact]
    public void FromDateInPast_ShouldNotHaveError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var query = new GetDashboardStatisticsQuery(from, null);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void FromDateBeforeToDate_BothInPast_ShouldNotHaveError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(-1);
        var query = new GetDashboardStatisticsQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void OnlyToDateProvided_ShouldNotHaveError()
    {
        var to = FixedUtcNow.AddDays(-1);
        var query = new GetDashboardStatisticsQuery(null, to);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
