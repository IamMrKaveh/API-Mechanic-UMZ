using Application.Review.Features.Commands.BulkOperation;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.BulkOperation;

public class BulkApproveReviewsValidatorTests
{
    private readonly BulkApproveReviewsValidator _sut = new();

    [Fact]
    public void EmptyReviewIds_ShouldHaveError()
    {
        var cmd = new BulkApproveReviewsCommand(Array.Empty<Guid>());

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewIds)
              .WithErrorMessage("حداقل یک شناسه نظر باید ارسال شود.");
    }

    [Fact]
    public void MoreThanOneHundredReviewIds_ShouldHaveError()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkApproveReviewsCommand(ids);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewIds)
              .WithErrorMessage("در هر درخواست حداکثر ۱۰۰ نظر قابل پردازش است.");
    }

    [Fact]
    public void ExactlyOneHundredReviewIds_ShouldNotHaveError()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkApproveReviewsCommand(ids);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewIds);
    }

    [Fact]
    public void ContainsEmptyGuidInsideCollection_ShouldHaveErrorOnItem()
    {
        var cmd = new BulkApproveReviewsCommand(new[] { Guid.NewGuid(), Guid.Empty });

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor("ReviewIds[1]")
              .WithErrorMessage("شناسه نظر نامعتبر است.");
    }

    [Fact]
    public void AllEmptyGuids_ShouldHaveErrorsOnEveryItem()
    {
        var cmd = new BulkApproveReviewsCommand(new[] { Guid.Empty, Guid.Empty });

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor("ReviewIds[0]");
        result.ShouldHaveValidationErrorFor("ReviewIds[1]");
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new BulkApproveReviewsCommand(new[] { Guid.NewGuid(), Guid.NewGuid() });

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SingleValidReviewId_ShouldNotHaveError()
    {
        var cmd = new BulkApproveReviewsCommand(new[] { Guid.NewGuid() });

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
