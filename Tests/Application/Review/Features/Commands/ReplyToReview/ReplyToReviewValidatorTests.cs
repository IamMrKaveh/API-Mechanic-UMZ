using Application.Review.Features.Commands.ReplyToReview;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.ReplyToReview;

public class ReplyToReviewValidatorTests
{
    private readonly ReplyToReviewValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveErrorWithPersianMessage()
    {
        var cmd = new ReplyToReviewCommand(Guid.Empty, "پاسخ معتبر");

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId)
              .WithErrorMessage("شناسه نظر الزامی است.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceReply_ShouldHaveErrorWithPersianMessage(string reply)
    {
        var cmd = new ReplyToReviewCommand(Guid.NewGuid(), reply);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reply)
              .WithErrorMessage("متن پاسخ الزامی است.");
    }

    [Fact]
    public void NullReply_ShouldHaveError()
    {
        var cmd = new ReplyToReviewCommand(Guid.NewGuid(), null!);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reply);
    }

    [Fact]
    public void ReplyAtMaxLength_ShouldNotHaveError()
    {
        var reply = new string('a', 1000);
        var cmd = new ReplyToReviewCommand(Guid.NewGuid(), reply);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reply);
    }

    [Fact]
    public void ReplyExceedingMaxLength_ShouldHaveError()
    {
        var reply = new string('a', 1001);
        var cmd = new ReplyToReviewCommand(Guid.NewGuid(), reply);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reply)
              .WithErrorMessage("متن پاسخ نباید بیش از ۱۰۰۰ کاراکتر باشد.");
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new ReplyToReviewCommand(Guid.NewGuid(), "این یک پاسخ معتبر است.");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
