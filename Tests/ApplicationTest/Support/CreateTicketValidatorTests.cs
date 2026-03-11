using Domain.Support.Aggregates;

namespace Tests.ApplicationTest.Support;

public class CreateTicketValidatorTests
{
    private readonly CreateTicketValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var command = new CreateTicketCommand(1, "مشکل در ثبت سفارش", Ticket.TicketPriorities.Normal, "لطفاً بررسی کنید.");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithZeroUserId_ShouldHaveError()
    {
        var command = new CreateTicketCommand(0, "موضوع", Ticket.TicketPriorities.Normal, "پیام");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithEmptySubject_ShouldHaveError()
    {
        var command = new CreateTicketCommand(1, "", Ticket.TicketPriorities.Normal, "پیام");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public void Validate_WithTooShortSubject_ShouldHaveError()
    {
        var command = new CreateTicketCommand(1, "کوت", Ticket.TicketPriorities.Normal, "پیام");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public void Validate_WithTooLongSubject_ShouldHaveError()
    {
        var longSubject = new string('a', 201);
        var command = new CreateTicketCommand(1, longSubject, Ticket.TicketPriorities.Normal, "پیام");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public void Validate_WithInvalidPriority_ShouldHaveError()
    {
        var command = new CreateTicketCommand(1, "موضوع معتبر", "INVALID_PRIORITY", "پیام");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Normal")]
    [InlineData("High")]
    [InlineData("Urgent")]
    public void Validate_WithValidPriority_ShouldNotHavePriorityError(string priority)
    {
        var command = new CreateTicketCommand(1, "موضوع معتبر", priority, "پیام");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void Validate_WithEmptyMessage_ShouldHaveError()
    {
        var command = new CreateTicketCommand(1, "موضوع معتبر", Ticket.TicketPriorities.Normal, "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void Validate_WithTooLongMessage_ShouldHaveError()
    {
        var longMessage = new string('a', 5001);
        var command = new CreateTicketCommand(1, "موضوع معتبر", Ticket.TicketPriorities.Normal, longMessage);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Message);
    }
}