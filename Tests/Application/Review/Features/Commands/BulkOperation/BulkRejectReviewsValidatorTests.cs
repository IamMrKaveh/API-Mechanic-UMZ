using Application.Review.Features.Commands.BulkOperation;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.BulkOperation;

public class BulkRejectReviewsValidatorTests
{
    private readonly BulkRejectReviewsValidator _sut = new();

    [Fact]
    public void EmptyReviewIds_ShouldHaveError()
    {
        var cmd = new BulkRejectReviewsCommand(Array.Empty<Guid>(), "reason");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReviewIds);
    }

    [Fact]
    public void MoreThanOneHundredReviewIds_ShouldHaveError()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkRejectReviewsCommand(ids, "reason");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReviewIds);
    }

    [Fact]
    public void EmptyReason_ShouldHaveError()
    {
        var cmd = new BulkRejectReviewsCommand(new[] { Guid.NewGuid() }, "");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonTooLong_ShouldHaveError()
    {
        var cmd = new BulkRejectReviewsCommand(new[] { Guid.NewGuid() }, new string('a', 501));
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveError()
    {
        var cmd = new BulkRejectReviewsCommand(new[] { Guid.NewGuid() }, "reason");
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyInnerReviewId_ShouldHaveError()
    {
        var cmd = new BulkRejectReviewsCommand(new[] { Guid.Empty }, "reason");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("ReviewIds[0]");
    }
}
