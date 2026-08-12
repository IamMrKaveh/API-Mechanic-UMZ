using Application.Analytics.Features.Queries.GetRevenueReport;
using FluentValidation.TestHelper;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Analytics.Features.Queries.GetRevenueReport;

public class GetRevenueReportValidatorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 08, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly GetRevenueReportValidator _sut =
        new(new FixedDateTimeProvider(FixedUtcNow));

    [Fact]
    public void FromDateDefault_ShouldHaveNotEmptyError()
    {
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetRevenueReportQuery(default, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع الزامی است.");
    }

    [Fact]
    public void ToDateDefault_ShouldHaveNotEmptyError()
    {
        var from = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetRevenueReportQuery(from, default);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.ToDate)
              .WithErrorMessage("تاریخ پایان الزامی است.");
    }

    [Fact]
    public void FromDateAfterToDate_ShouldHaveError()
    {
        var from = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetRevenueReportQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void ToDateBeyondTomorrow_ShouldHaveError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(2);
        var query = new GetRevenueReportQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.ToDate)
              .WithErrorMessage("تاریخ پایان نمی‌تواند در آینده باشد.");
    }

    [Fact]
    public void ToDateEqualsTomorrow_ShouldNotHaveError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(1);
        var query = new GetRevenueReportQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    [Fact]
    public void ValidRange_ShouldNotHaveAnyError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(-1);
        var query = new GetRevenueReportQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
