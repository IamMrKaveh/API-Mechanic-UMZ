using Domain.Support.Aggregates;

namespace Tests.DomainTest.Support;

public class TicketCreationTests
{
    [Fact]
    public void Open_WithValidParameters_ShouldCreateTicket()
    {
        var ticket = new TicketBuilder().Build();

        ticket.Should().NotBeNull();
        ticket.Subject.Should().Be("مشکل در سفارش");
        ticket.UserId.Should().Be(1);
    }

    [Fact]
    public void Open_ShouldSetStatusToOpen()
    {
        var ticket = new TicketBuilder().Build();

        ticket.IsOpen.Should().BeTrue();
        ticket.Status.Should().Be(Ticket.TicketStatuses.Open);
    }

    [Fact]
    public void Open_ShouldSetDefaultPriority()
    {
        var ticket = new TicketBuilder().Build();

        ticket.Priority.Should().Be(Ticket.TicketPriorities.Normal);
    }

    [Fact]
    public void Open_ShouldAddInitialMessage()
    {
        var ticket = new TicketBuilder().Build();

        ticket.MessageCount.Should().Be(1);
        ticket.Messages.Should().HaveCount(1);
    }

    [Fact]
    public void Open_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var ticket = new TicketBuilder().Build();

        ticket.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Open_ShouldRaiseTicketCreatedEvent()
    {
        var ticket = new TicketBuilder().Build();

        ticket.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TicketCreatedEvent");
    }

    [Fact]
    public void Open_WithEmptySubject_ShouldThrowDomainException()
    {
        var act = () => Ticket.Open(1, "", Ticket.TicketPriorities.Normal, "پیام");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Open_WithTooShortSubject_ShouldThrowDomainException()
    {
        var act = () => Ticket.Open(1, "کوت", Ticket.TicketPriorities.Normal, "پیام");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Open_WithTooLongSubject_ShouldThrowDomainException()
    {
        var longSubject = new string('a', 201);
        var act = () => Ticket.Open(1, longSubject, Ticket.TicketPriorities.Normal, "پیام");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Open_WithEmptyMessage_ShouldThrowDomainException()
    {
        var act = () => Ticket.Open(1, "موضوع تیکت تست", Ticket.TicketPriorities.Normal, "");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Open_WithZeroUserId_ShouldThrowException()
    {
        var act = () => Ticket.Open(0, "موضوع تیکت", Ticket.TicketPriorities.Normal, "پیام اول");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Open_WithInvalidPriority_ShouldThrowDomainException()
    {
        var act = () => Ticket.Open(1, "موضوع تیکت", "InvalidPriority", "پیام");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void OpenWithDefaultPriority_ShouldSetNormalPriority()
    {
        var ticket = Ticket.OpenWithDefaultPriority(1, "موضوع معتبر", "پیام اول");

        ticket.Priority.Should().Be(Ticket.TicketPriorities.Normal);
    }

    [Fact]
    public void Open_WithHighPriority_ShouldSetPriority()
    {
        var ticket = new TicketBuilder().AsHighPriority().Build();

        ticket.Priority.Should().Be(Ticket.TicketPriorities.High);
    }

    [Fact]
    public void Open_WithUrgentPriority_ShouldSetPriority()
    {
        var ticket = new TicketBuilder().AsUrgent().Build();

        ticket.Priority.Should().Be(Ticket.TicketPriorities.Urgent);
    }

    [Fact]
    public void Open_MessageContent_ShouldMatchInitialMessage()
    {
        var message = "این یک پیام تست می‌باشد.";

        var ticket = new TicketBuilder().WithMessage(message).Build();

        ticket.Messages.First().Content.Should().Be(message);
    }
}