using Application.Review.Features.Queries.GetReviewsByStatus;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Queries.GetReviewsByStatus;

public class GetReviewsByStatusValidatorTests
{
    private readonly GetReviewsByStatusValidator _sut = new();

    [Fact]
    public void EmptyStatus_ShouldHaveError()
    {
        var q = new GetReviewsByStatusQuery("");
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    [InlineData("All")]
    public void KnownStatus_ShouldNotHaveError(string status)
    {
        var q = new GetReviewsByStatusQuery(status);
        var result = _sut.TestValidate(q);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void UnknownStatus_ShouldHaveError()
    {
        var q = new GetReviewsByStatusQuery("Bogus");
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageMustBeGreaterThanZero(int page)
    {
        var q = new GetReviewsByStatusQuery("Approved", page);
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSizeMustBeBetweenOneAndOneHundred(int pageSize)
    {
        var q = new GetReviewsByStatusQuery("Approved", 1, pageSize);
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void MinRatingOutOfRange_ShouldHaveError(int minRating)
    {
        var q = new GetReviewsByStatusQuery("Approved", MinRating: minRating);
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x.MinRating!.Value);
    }

    [Fact]
    public void SearchText_TooLong_ShouldHaveError()
    {
        var q = new GetReviewsByStatusQuery("Approved", SearchText: new string('a', 201));
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x.SearchText!);
    }

    [Fact]
    public void DateRange_FromAfterTo_ShouldHaveError()
    {
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var q = new GetReviewsByStatusQuery("Approved", DateFrom: from, DateTo: to);
        var result = _sut.TestValidate(q);
        result.ShouldHaveValidationErrorFor(x => x);
    }
}
