using Application.Review.Features.Commands.DeleteReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.DeleteReview;

public class DeleteReviewValidatorTests
{
    private readonly DeleteReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new DeleteReviewCommand(Guid.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidReviewId_ShouldNotHaveError()
    {
        var cmd = new DeleteReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ReasonNull_ShouldNotHaveErrorOnReason()
    {
        var cmd = new DeleteReviewCommand(Guid.NewGuid(), null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReasonWhitespaceOrEmpty_ShouldNotHaveErrorOnReason(string reason)
    {
        var cmd = new DeleteReviewCommand(Guid.NewGuid(), reason);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonAtMaxLength_ShouldNotHaveError()
    {
        var reason = new string('a', 500);
        var cmd = new DeleteReviewCommand(Guid.NewGuid(), reason);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonExceedingMaxLength_ShouldHaveError()
    {
        var reason = new string('a', 501);
        var cmd = new DeleteReviewCommand(Guid.NewGuid(), reason);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("دلیل حذف نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }

    [Fact]
    public void ValidCommandWithoutReason_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new DeleteReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidCommandWithReason_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new DeleteReviewCommand(Guid.NewGuid(), "spam content");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
