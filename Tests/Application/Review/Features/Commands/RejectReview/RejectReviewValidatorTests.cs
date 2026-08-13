using Application.Review.Features.Commands.RejectReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.RejectReview;

public class RejectReviewValidatorTests
{
    private readonly RejectReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new RejectReviewCommand(Guid.Empty, "reason");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void EmptyReason_ShouldHaveError()
    {
        var cmd = new RejectReviewCommand(Guid.NewGuid(), "");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("دلیل رد الزامی است.");
    }

    [Fact]
    public void ReasonTooLong_ShouldHaveError()
    {
        var cmd = new RejectReviewCommand(Guid.NewGuid(), new string('a', 501));
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("دلیل رد نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveError()
    {
        var cmd = new RejectReviewCommand(Guid.NewGuid(), "reason");
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}
