using Application.Review.Features.Commands.DislikeReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.DislikeReview;

public class DislikeReviewValidatorTests
{
    private readonly DislikeReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveErrorWithPersianMessage()
    {
        var cmd = new DislikeReviewCommand(Guid.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId)
              .WithErrorMessage("شناسه نظر الزامی است.");
    }

    [Fact]
    public void ValidReviewId_ShouldNotHaveError()
    {
        var cmd = new DislikeReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new DislikeReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
