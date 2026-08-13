using Application.Support.Features.Commands.CloseTicket;

namespace Tests.Application.Support.Features.Commands.CloseTicket;

public class CloseTicketValidatorTests
{
    private readonly CloseTicketValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyTicketId_HasValidationError()
    {
        var command = new CloseTicketCommand(Guid.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CloseTicketCommand.TicketId));
    }

    [Fact]
    public void Validate_WithNonEmptyTicketId_IsValid()
    {
        var command = new CloseTicketCommand(Guid.NewGuid());

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
