using Application.Analytics.Features.Queries.GetSalesChartData;
using FluentValidation.TestHelper;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Analytics.Features.Queries.GetSalesChartData;

public class GetSalesChartDataValidatorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 08, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly GetSalesChartDataValidator _sut =
        new(new FixedDateTimeProvider(FixedUtcNow));

    [Fact]
    public void FromDateDefault_ShouldHaveNotEmptyError()
    {
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetSalesChartDataQuery(default, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع الزامی است.");
    }

    [Fact]
    public void ToDateDefault_ShouldHaveNotEmptyError()
    {
        var from = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetSalesChartDataQuery(from, default);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.ToDate)
              .WithErrorMessage("تاریخ پایان الزامی است.");
    }

    [Fact]
    public void FromDateAfterToDate_ShouldHaveError()
    {
        var from = FixedUtcNow.AddDays(-1);
        var to = FixedUtcNow.AddDays(-10);
        var query = new GetSalesChartDataQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void ToDateBeyondTomorrow_ShouldHaveError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(2);
        var query = new GetSalesChartDataQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.ToDate)
              .WithErrorMessage("تاریخ پایان نمی‌تواند در آینده باشد.");
    }

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    [InlineData("DAY")]
    [InlineData("Week")]
    [InlineData("MONTH")]
    public void GroupBy_WithAllowedValueRegardlessOfCase_ShouldNotHaveError(string groupBy)
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(-1);
        var query = new GetSalesChartDataQuery(from, to, groupBy);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.GroupBy);
    }

    [Theory]
    [InlineData("year")]
    [InlineData("hour")]
    [InlineData("quarter")]
    [InlineData("")]
    public void GroupBy_WithDisallowedValue_ShouldHaveError(string groupBy)
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(-1);
        var query = new GetSalesChartDataQuery(from, to, groupBy);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.GroupBy)
              .WithErrorMessage("مقدار groupBy باید یکی از day, week یا month باشد.");
    }

    [Fact]
    public void ValidQuery_ShouldNotHaveAnyError()
    {
        var from = FixedUtcNow.AddDays(-30);
        var to = FixedUtcNow.AddDays(-1);
        var query = new GetSalesChartDataQuery(from, to, "day");

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
        result.ShouldNotHaveValidationErrorFor(x => x.GroupBy);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
