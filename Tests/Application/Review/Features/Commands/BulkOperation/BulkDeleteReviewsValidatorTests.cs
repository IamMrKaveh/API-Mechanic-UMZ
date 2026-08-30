using Application.Review.Features.Commands.BulkOperation;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.BulkOperation;

public class BulkDeleteReviewsValidatorTests
{
    private readonly BulkDeleteReviewsValidator _sut = new();

    [Fact]
    public void EmptyReviewIds_ShouldHaveError()
    {
        var cmd = new BulkDeleteReviewsCommand(Array.Empty<Guid>());

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewIds)
              .WithErrorMessage("حداقل یک شناسه نظر باید ارسال شود.");
    }

    [Fact]
    public void MoreThanOneHundredReviewIds_ShouldHaveError()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkDeleteReviewsCommand(ids);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewIds)
              .WithErrorMessage("در هر درخواست حداکثر ۱۰۰ نظر قابل پردازش است.");
    }

    [Fact]
    public void ExactlyOneHundredReviewIds_ShouldNotHaveError()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkDeleteReviewsCommand(ids);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ReviewIds);
    }

    [Fact]
    public void ContainsEmptyGuidInsideCollection_ShouldHaveErrorOnItem()
    {
        var cmd = new BulkDeleteReviewsCommand(new[] { Guid.NewGuid(), Guid.Empty });

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor("ReviewIds[1]")
              .WithErrorMessage("شناسه نظر نامعتبر است.");
    }

    [Fact]
    public void ReasonNull_ShouldNotHaveErrorOnReason()
    {
        var cmd = new BulkDeleteReviewsCommand(new[] { Guid.NewGuid() }, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReasonNullOrWhitespace_ShouldNotHaveErrorOnReason(string reason)
    {
        var cmd = new BulkDeleteReviewsCommand(new[] { Guid.NewGuid() }, reason);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonWithinLimit_ShouldNotHaveError()
    {
        var reason = new string('a', 500);
        var cmd = new BulkDeleteReviewsCommand(new[] { Guid.NewGuid() }, reason);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonExceedingMaxLength_ShouldHaveError()
    {
        var reason = new string('a', 501);
        var cmd = new BulkDeleteReviewsCommand(new[] { Guid.NewGuid() }, reason);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("دلیل حذف نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new BulkDeleteReviewsCommand(new[] { Guid.NewGuid(), Guid.NewGuid() }, "reason");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
