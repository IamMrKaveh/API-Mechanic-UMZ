using Application.Review.Features.Commands.UpdateReviewStatus;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.UpdateReviewStatus;

public class UpdateReviewStatusValidatorTests
{
    private readonly UpdateReviewStatusValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new UpdateReviewStatusCommand(Guid.Empty, "Approved");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Fact]
    public void EmptyStatus_ShouldHaveError()
    {
        var cmd = new UpdateReviewStatusCommand(Guid.NewGuid(), "");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Deleted")]
    [InlineData("Bogus")]
    public void UnknownStatus_ShouldHaveError(string status)
    {
        var cmd = new UpdateReviewStatusCommand(Guid.NewGuid(), status);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("approved")]
    [InlineData("Rejected")]
    [InlineData("rejected")]
    public void KnownStatus_ShouldNotHaveError(string status)
    {
        var reason = string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase) ? "reason" : null;
        var cmd = new UpdateReviewStatusCommand(Guid.NewGuid(), status, reason);
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void RejectedWithoutReason_ShouldHaveError()
    {
        var cmd = new UpdateReviewStatusCommand(Guid.NewGuid(), "Rejected", null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("در صورت رد نظر، ذکر دلیل الزامی است.");
    }

    [Fact]
    public void ApprovedWithoutReason_ShouldNotHaveErrorOnReason()
    {
        var cmd = new UpdateReviewStatusCommand(Guid.NewGuid(), "Approved", null);
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonExceedingMaxLength_ShouldHaveError()
    {
        var longReason = new string('a', 501);
        var cmd = new UpdateReviewStatusCommand(Guid.NewGuid(), "Rejected", longReason);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
