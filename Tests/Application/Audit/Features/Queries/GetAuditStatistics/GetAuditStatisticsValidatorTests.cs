using Application.Audit.Features.Queries.GetAuditStatistics;

namespace Tests.Application.Audit.Features.Queries.GetAuditStatistics;

public class GetAuditStatisticsValidatorTests
{
    private readonly GetAuditStatisticsValidator _sut = new();

    private static GetAuditStatisticsQuery ValidQuery(DateTime? from = null, DateTime? to = null) =>
        new(from, to);

    [Fact]
    public void Validate_WithNoDates_IsValid()
    {
        var result = _sut.Validate(ValidQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyFromSpecified_IsValid()
    {
        var result = _sut.Validate(ValidQuery(from: DateTime.UtcNow.AddDays(-30)));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyToSpecified_IsValid()
    {
        var result = _sut.Validate(ValidQuery(to: DateTime.UtcNow));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromBeforeTo_IsValid()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow.AddDays(-1);

        var result = _sut.Validate(ValidQuery(from: from, to: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromEqualToTo_IsValid()
    {
        var timestamp = DateTime.UtcNow.AddDays(-1);

        var result = _sut.Validate(ValidQuery(from: timestamp, to: timestamp));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromAfterTo_FailsOnDateRange()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(-10);

        var result = _sut.Validate(ValidQuery(from: from, to: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "DateRange");
    }

    [Fact]
    public void Validate_WithRangeExactlyAtMaximumSpan_IsValid()
    {
        var from = DateTime.UtcNow.AddDays(-366);
        var to = DateTime.UtcNow;

        var result = _sut.Validate(ValidQuery(from: from, to: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithRangeExceedingMaximumSpan_FailsOnDateRangeSpan()
    {
        var from = DateTime.UtcNow.AddDays(-400);
        var to = DateTime.UtcNow;

        var result = _sut.Validate(ValidQuery(from: from, to: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "DateRangeSpan");
    }

    [Fact]
    public void Validate_WithFromInTheFuture_FailsOnFrom()
    {
        var futureFrom = DateTime.UtcNow.AddDays(5);
        var futureTo = DateTime.UtcNow.AddDays(10);

        var result = _sut.Validate(ValidQuery(from: futureFrom, to: futureTo));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditStatisticsQuery.From));
    }

    [Fact]
    public void Validate_WithToMoreThanOneDayInTheFuture_FailsOnTo()
    {
        var to = DateTime.UtcNow.AddDays(3);

        var result = _sut.Validate(ValidQuery(to: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditStatisticsQuery.To));
    }

    [Fact]
    public void Validate_WithToSlightlyInTheFutureWithinToleranceWindow_IsValid()
    {
        var to = DateTime.UtcNow.AddHours(6);

        var result = _sut.Validate(ValidQuery(to: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromInFutureAndToInFuture_ReturnsMultipleErrors()
    {
        var from = DateTime.UtcNow.AddDays(10);
        var to = DateTime.UtcNow.AddDays(20);

        var result = _sut.Validate(ValidQuery(from: from, to: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditStatisticsQuery.From));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditStatisticsQuery.To));
    }
}
