using Application.Support.Features.Commands.CreateTicket;

namespace Tests.Application.Support.Features.Commands.CreateTicket;

public class CreateTicketValidatorTests
{
    private readonly CreateTicketValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new CreateTicketCommand(
            Subject: "Order help",
            Category: "Billing",
            Priority: "Normal",
            Message: "Hello, I need help.");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptySubject_HasValidationError(string subject)
    {
        var command = new CreateTicketCommand(subject, "Billing", null, "Hi");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTicketCommand.Subject));
    }

    [Fact]
    public void Validate_WithSubjectExceedingMaxLength_HasValidationError()
    {
        var subject = new string('a', 201);
        var command = new CreateTicketCommand(subject, "Billing", null, "Hi");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTicketCommand.Subject));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyCategory_HasValidationError(string category)
    {
        var command = new CreateTicketCommand("Subject", category, null, "Hi");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTicketCommand.Category));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyMessage_HasValidationError(string message)
    {
        var command = new CreateTicketCommand("Subject", "Billing", null, message);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTicketCommand.Message));
    }

    [Fact]
    public void Validate_WithMessageExceedingMaxLength_HasValidationError()
    {
        var message = new string('a', 5001);
        var command = new CreateTicketCommand("Subject", "Billing", null, message);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTicketCommand.Message));
    }

    [Fact]
    public void Validate_WithNullPriority_IsValid()
    {
        var command = new CreateTicketCommand("Subject", "Billing", null, "Hi");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
