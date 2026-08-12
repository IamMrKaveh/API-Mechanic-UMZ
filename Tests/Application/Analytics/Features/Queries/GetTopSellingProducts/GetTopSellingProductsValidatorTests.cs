using Application.Analytics.Features.Queries.GetTopSellingProducts;
using FluentValidation.TestHelper;

namespace Tests.Application.Analytics.Features.Queries.GetTopSellingProducts;

public class GetTopSellingProductsValidatorTests
{
    private readonly GetTopSellingProductsValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    [InlineData(200)]
    public void Count_OutsideAllowedRange_ShouldHaveError(int count)
    {
        var query = new GetTopSellingProductsQuery(count);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Count)
              .WithErrorMessage("تعداد باید بین ۱ و ۱۰۰ باشد.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Count_WithinAllowedRange_ShouldNotHaveError(int count)
    {
        var query = new GetTopSellingProductsQuery(count);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void BothDatesNull_ShouldNotHaveError()
    {
        var query = new GetTopSellingProductsQuery(10, null, null);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void OnlyFromDateProvided_ShouldNotHaveError()
    {
        var query = new GetTopSellingProductsQuery(
            10, new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc), null);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void OnlyToDateProvided_ShouldNotHaveError()
    {
        var query = new GetTopSellingProductsQuery(
            10, null, new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc));

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public void FromDateAfterToDate_ShouldHaveError()
    {
        var from = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetTopSellingProductsQuery(10, from, to);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void FromDateEqualsToDate_ShouldHaveError()
    {
        var same = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetTopSellingProductsQuery(10, same, same);

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
              .WithErrorMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
    }

    [Fact]
    public void FromDateBeforeToDate_ShouldNotHaveError()
    {
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetTopSellingProductsQuery(10, from, to);

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }
}
