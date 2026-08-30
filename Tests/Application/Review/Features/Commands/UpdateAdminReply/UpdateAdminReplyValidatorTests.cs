using Application.Review.Features.Commands.UpdateAdminReply;
using FluentValidation.TestHelper;

namespace Tests.Application.Review.Features.Commands.UpdateAdminReply;

public class UpdateAdminReplyValidatorTests
{
    private readonly UpdateAdminReplyValidator _sut = new();

    [Fact]
    public void EmptyReviewId_ShouldHaveError()
    {
        var cmd = new UpdateAdminReplyCommand(Guid.Empty, "پاسخ به‌روزرسانی شده");

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceReply_ShouldHaveErrorWithPersianMessage(string reply)
    {
        var cmd = new UpdateAdminReplyCommand(Guid.NewGuid(), reply);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reply)
              .WithErrorMessage("متن پاسخ الزامی است.");
    }

    [Fact]
    public void NullReply_ShouldHaveError()
    {
        var cmd = new UpdateAdminReplyCommand(Guid.NewGuid(), null!);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reply);
    }

    [Fact]
    public void ReplyAtMaxLength_ShouldNotHaveError()
    {
        var reply = new string('a', 1000);
        var cmd = new UpdateAdminReplyCommand(Guid.NewGuid(), reply);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Reply);
    }

    [Fact]
    public void ReplyExceedingMaxLength_ShouldHaveError()
    {
        var reply = new string('a', 1001);
        var cmd = new UpdateAdminReplyCommand(Guid.NewGuid(), reply);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reply)
              .WithErrorMessage("متن پاسخ نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var cmd = new UpdateAdminReplyCommand(Guid.NewGuid(), "پاسخ معتبر");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
