using Application.Review.Features.Commands.UpdateOwnReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.UpdateOwnReview;

public class UpdateOwnReviewValidatorTests
{
    private readonly UpdateOwnReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new UpdateOwnReviewCommand(Guid.Empty, 5, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Rating_OutOfRange_ShouldHaveError(int rating)
    {
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), rating, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Rating)
              .WithErrorMessage("امتیاز باید بین ۱ تا ۵ باشد.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Rating_InRange_ShouldNotHaveError(int rating)
    {
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), rating, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Title_Null_ShouldNotHaveError()
    {
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 5, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_AtMaxLength_ShouldNotHaveError()
    {
        var title = new string('a', 100);
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 5, title, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_LongerThanMaxLength_ShouldHaveError()
    {
        var title = new string('a', 101);
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 5, title, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Comment_Null_ShouldNotHaveError()
    {
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 5, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void Comment_AtMaxLength_ShouldNotHaveError()
    {
        var comment = new string('a', 1000);
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 5, null, comment);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void Comment_LongerThanMaxLength_ShouldHaveError()
    {
        var comment = new string('a', 1001);
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 5, null, comment);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void EmptyTitleAndComment_ShouldNotHaveError()
    {
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 3, string.Empty, string.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new UpdateOwnReviewCommand(Guid.NewGuid(), 4, "عنوان معتبر", "توضیح معتبر");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MultipleInvalidFields_ShouldReportAllErrors()
    {
        var cmd = new UpdateOwnReviewCommand(
            Guid.Empty,
            0,
            new string('a', 101),
            new string('b', 1001));

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
