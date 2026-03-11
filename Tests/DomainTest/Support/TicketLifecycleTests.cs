using Domain.Support.Aggregates;

namespace Tests.DomainTest.Support;

public class TicketLifecycleTests
{
    [Fact]
    public void Close_WhenOpen_ShouldSetStatusToClosed()
    {
        var ticket = new TicketBuilder().Build();

        ticket.Close();

        ticket.IsClosed.Should().BeTrue();
        ticket.Status.Should().Be(Ticket.TicketStatuses.Closed);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ShouldNotThrow()
    {
        var ticket = new TicketBuilder().BuildClosed();

        var act = () => ticket.Close();

        act.Should().NotThrow();
    }

    [Fact]
    public void Close_ShouldRaiseTicketClosedEvent()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.Close();

        ticket.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TicketClosedEvent");
    }

    [Fact]
    public void Close_ShouldSetUpdatedAt()
    {
        var ticket = new TicketBuilder().Build();

        ticket.Close();

        ticket.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reopen_WhenClosed_ShouldSetStatusToOpen()
    {
        var ticket = new TicketBuilder().BuildClosed();

        ticket.Reopen();

        ticket.IsOpen.Should().BeTrue();
        ticket.Status.Should().Be(Ticket.TicketStatuses.Open);
    }

    [Fact]
    public void Reopen_WhenAlreadyOpen_ShouldThrowDomainException()
    {
        var ticket = new TicketBuilder().Build();

        var act = () => ticket.Reopen();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reopen_ShouldRaiseTicketReopenedEvent()
    {
        var ticket = new TicketBuilder().BuildClosed();
        ticket.ClearDomainEvents();

        ticket.Reopen();

        ticket.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TicketReopenedEvent");
    }

    [Fact]
    public void AddMessage_WhenOpen_ShouldAddMessage()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage("پیام دوم", isAdminReply: false);

        ticket.MessageCount.Should().Be(2);
    }

    [Fact]
    public void AddMessage_ByAdmin_ShouldSetStatusToAnswered()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage("پاسخ ادمین", isAdminReply: true);

        ticket.IsAnswered.Should().BeTrue();
        ticket.Status.Should().Be(Ticket.TicketStatuses.Answered);
    }

    [Fact]
    public void AddMessage_ByUser_ShouldSetStatusToAwaitingReply()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage("پاسخ ادمین", isAdminReply: true);

        ticket.AddMessage("پیام جدید کاربر", isAdminReply: false);

        ticket.IsAwaitingReply.Should().BeTrue();
    }

    [Fact]
    public void AddMessage_WhenClosed_ShouldThrowException()
    {
        var ticket = new TicketBuilder().BuildClosed();

        var act = () => ticket.AddMessage("پیام جدید", isAdminReply: false);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddMessage_ByAdmin_ShouldRaiseTicketAnsweredEvent()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.AddMessage("پاسخ ادمین", isAdminReply: true, senderId: 99);

        ticket.DomainEvents.Should().Contain(e => e.GetType().Name == "TicketAnsweredEvent");
    }

    [Fact]
    public void UpdateSubject_WhenOpen_ShouldUpdateSubject()
    {
        var ticket = new TicketBuilder().Build();

        ticket.UpdateSubject("موضوع جدید تیکت");

        ticket.Subject.Should().Be("موضوع جدید تیکت");
    }

    [Fact]
    public void UpdateSubject_WhenClosed_ShouldThrowException()
    {
        var ticket = new TicketBuilder().BuildClosed();

        var act = () => ticket.UpdateSubject("موضوع جدید");

        act.Should().Throw<Exception>();
    }
}