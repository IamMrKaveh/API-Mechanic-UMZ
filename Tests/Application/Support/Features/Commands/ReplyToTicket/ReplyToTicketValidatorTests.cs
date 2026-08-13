using Application.Support.Features.Commands.ReplyToTicket;

namespace Tests.Application.Support.Features.Commands.ReplyToTicket;

public class ReplyToTicketValidatorTests
{
    private readonly ReplyToTicketValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new ReplyToTicketCommand(Guid.NewGuid(), "Reply content");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTicketId_HasValidationError()
    {
        var command = new ReplyToTicketCommand(Guid.Empty, "Reply content");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReplyToTicketCommand.TicketId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyContent_HasValidationError(string content)
    {
        var command = new ReplyToTicketCommand(Guid.NewGuid(), content);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReplyToTicketCommand.Content));
    }

    [Fact]
    public void Validate_WithContentExceedingMaxLength_HasValidationError()
    {
        var content = new string('a', 5001);
        var command = new ReplyToTicketCommand(Guid.NewGuid(), content);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReplyToTicketCommand.Content));
    }
}
