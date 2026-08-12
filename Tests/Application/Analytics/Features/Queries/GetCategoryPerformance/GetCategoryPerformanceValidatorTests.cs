using Application.Analytics.Features.Queries.GetCategoryPerformance;
using FluentValidation.TestHelper;

namespace Tests.Application.Analytics.Features.Queries.GetCategoryPerformance;

public class GetCategoryPerformanceValidatorTests
{
    private readonly GetCategoryPerformanceValidator _sut = new();

    [Fact]
    public void BothDatesNull_ShouldNotHaveError()
    {
        var query = new GetCategoryPerformanceQuery(null, null);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void OnlyFromDateProvided_ShouldNotHaveError()
    {
        var query = new GetCategoryPerformanceQuery(new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc), null);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void OnlyToDateProvided_ShouldNotHaveError()
    {
        var query = new GetCategoryPerformanceQuery(null, new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc));

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void FromDateAfterToDate_ShouldHaveError()
    {
        var from = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetCategoryPerformanceQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void FromDateEqualsToDate_ShouldHaveError()
    {
        var same = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetCategoryPerformanceQuery(same, same);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void FromDateBeforeToDate_ShouldNotHaveError()
    {
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetCategoryPerformanceQuery(from, to);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }
}
