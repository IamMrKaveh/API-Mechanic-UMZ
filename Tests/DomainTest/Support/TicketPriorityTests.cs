using Domain.Support.Aggregates;

namespace Tests.DomainTest.Support;

public class TicketPriorityTests
{
    [Fact]
    public void ChangePriority_WithValidPriority_ShouldUpdatePriority()
    {
        var ticket = new TicketBuilder().Build();

        ticket.ChangePriority(Ticket.TicketPriorities.High);

        ticket.Priority.Should().Be(Ticket.TicketPriorities.High);
    }

    [Fact]
    public void ChangePriority_ToSamePriority_ShouldNotRaiseEvent()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.ChangePriority(Ticket.TicketPriorities.Normal);

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangePriority_ToDifferentPriority_ShouldRaisePriorityChangedEvent()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.ChangePriority(Ticket.TicketPriorities.Urgent);

        ticket.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TicketPriorityChangedEvent");
    }

    [Fact]
    public void ChangePriority_WithInvalidPriority_ShouldThrowDomainException()
    {
        var ticket = new TicketBuilder().Build();

        var act = () => ticket.ChangePriority("INVALID");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("High")]
    [InlineData("Urgent")]
    public void IsHighPriority_WithHighOrUrgent_ShouldReturnTrue(string priority)
    {
        var ticket = new TicketBuilder().WithPriority(priority).Build();

        ticket.IsHighPriority().Should().BeTrue();
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Normal")]
    public void IsHighPriority_WithLowOrNormal_ShouldReturnFalse(string priority)
    {
        var ticket = new TicketBuilder().WithPriority(priority).Build();

        ticket.IsHighPriority().Should().BeFalse();
    }

    [Fact]
    public void IsUrgent_WithUrgentPriority_ShouldReturnTrue()
    {
        var ticket = new TicketBuilder().AsUrgent().Build();

        ticket.IsUrgent().Should().BeTrue();
    }

    [Fact]
    public void IsUrgent_WithNonUrgentPriority_ShouldReturnFalse()
    {
        var ticket = new TicketBuilder().Build();

        ticket.IsUrgent().Should().BeFalse();
    }

    [Fact]
    public void RequiresUrgentAttention_WhenUrgentAndOpen_ShouldReturnTrue()
    {
        var ticket = new TicketBuilder().AsUrgent().Build();

        ticket.RequiresUrgentAttention().Should().BeTrue();
    }

    [Fact]
    public void RequiresUrgentAttention_WhenClosed_ShouldReturnFalse()
    {
        var ticket = new TicketBuilder().AsUrgent().BuildClosed();

        ticket.RequiresUrgentAttention().Should().BeFalse();
    }

    [Fact]
    public void ChangePriority_ShouldUpdateUpdatedAt()
    {
        var ticket = new TicketBuilder().Build();

        ticket.ChangePriority(Ticket.TicketPriorities.High);

        ticket.UpdatedAt.Should().NotBeNull();
    }
}