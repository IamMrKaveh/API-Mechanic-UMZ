using Application.Review.Features.Commands.ApproveReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.ApproveReview;

public class ApproveReviewValidatorTests
{
    private readonly ApproveReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new ApproveReviewCommand(Guid.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidReviewId_ShouldNotHaveError()
    {
        var cmd = new ApproveReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new ApproveReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
