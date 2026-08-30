using Application.Review.Features.Commands.RestoreReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.RestoreReview;

public class RestoreReviewValidatorTests
{
    private readonly RestoreReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new RestoreReviewCommand(Guid.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidReviewId_ShouldNotHaveError()
    {
        var cmd = new RestoreReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new RestoreReviewCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
