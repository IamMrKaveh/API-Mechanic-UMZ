using Application.Review.Features.Commands.CreateReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.CreateReview;

public class CreateReviewValidatorTests
{
    private readonly CreateReviewValidator _sut = new();

    [Fact]
    public void EmptyProductId_ShouldHaveError()
    {
        var cmd = new CreateReviewCommand(Guid.Empty, null, 5, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public void Rating_OutOfRange_ShouldHaveError(int rating)
    {
        var cmd = new CreateReviewCommand(Guid.NewGuid(), null, rating, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Rating)
              .WithErrorMessage("امتیاز باید بین ۱ تا ۵ باشد.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Rating_InRange_ShouldNotHaveError(int rating)
    {
        var cmd = new CreateReviewCommand(Guid.NewGuid(), null, rating, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Title_LongerThan100Chars_ShouldHaveError()
    {
        var longTitle = new string('a', 101);
        var cmd = new CreateReviewCommand(Guid.NewGuid(), null, 5, longTitle, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_Null_ShouldNotHaveError()
    {
        var cmd = new CreateReviewCommand(Guid.NewGuid(), null, 5, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Comment_LongerThan1000Chars_ShouldHaveError()
    {
        var longComment = new string('a', 1001);
        var cmd = new CreateReviewCommand(Guid.NewGuid(), null, 5, null, longComment);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
