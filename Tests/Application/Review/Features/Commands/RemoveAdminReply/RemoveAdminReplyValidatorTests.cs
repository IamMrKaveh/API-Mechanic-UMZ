using Application.Review.Features.Commands.RemoveAdminReply;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.RemoveAdminReply;

public class RemoveAdminReplyValidatorTests
{
    private readonly RemoveAdminReplyValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new RemoveAdminReplyCommand(Guid.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidReviewId_ShouldNotHaveError()
    {
        var cmd = new RemoveAdminReplyCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new RemoveAdminReplyCommand(Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
