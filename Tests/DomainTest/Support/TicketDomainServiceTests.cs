using Domain.Support.Aggregates;

namespace Tests.DomainTest.Support;

public class TicketDomainServiceTests
{
    private readonly TicketDomainService _service = new();

    [Fact]
    public void ValidateUserAccess_WhenAdmin_ShouldGrantAccess()
    {
        var ticket = new TicketBuilder().WithUserId(1).Build();

        var (hasAccess, error) = _service.ValidateUserAccess(ticket, userId: 99, isAdmin: true);

        hasAccess.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateUserAccess_WhenOwner_ShouldGrantAccess()
    {
        var ticket = new TicketBuilder().WithUserId(5).Build();

        var (hasAccess, _) = _service.ValidateUserAccess(ticket, userId: 5, isAdmin: false);

        hasAccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateUserAccess_WhenNotOwnerAndNotAdmin_ShouldDenyAccess()
    {
        var ticket = new TicketBuilder().WithUserId(1).Build();

        var (hasAccess, error) = _service.ValidateUserAccess(ticket, userId: 99, isAdmin: false);

        hasAccess.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateCanSendMessage_WhenTicketOpen_ShouldAllowSending()
    {
        var ticket = new TicketBuilder().Build();

        var (canSend, _) = _service.ValidateCanSendMessage(ticket);

        canSend.Should().BeTrue();
    }

    [Fact]
    public void ValidateCanSendMessage_WhenTicketClosed_ShouldDenySending()
    {
        var ticket = new TicketBuilder().BuildClosed();

        var (canSend, error) = _service.ValidateCanSendMessage(ticket);

        canSend.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateCanClose_WhenTicketOpen_ShouldAllowClosing()
    {
        var ticket = new TicketBuilder().Build();

        var (canClose, _) = _service.ValidateCanClose(ticket);

        canClose.Should().BeTrue();
    }

    [Fact]
    public void ValidateCanClose_WhenAlreadyClosed_ShouldDenyClosing()
    {
        var ticket = new TicketBuilder().BuildClosed();

        var (canClose, error) = _service.ValidateCanClose(ticket);

        canClose.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CalculateStatistics_ShouldReturnCorrectCounts()
    {
        var tickets = new List<Ticket>
        {
            new TicketBuilder().Build(),
            new TicketBuilder().Build(),
            new TicketBuilder().BuildClosed(),
            new TicketBuilder().AsUrgent().Build()
        };

        var stats = _service.CalculateStatistics(tickets);

        stats.Total.Should().Be(4);
        stats.Closed.Should().Be(1);
        stats.Urgent.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SuggestPriority_WithUrgentKeyword_ShouldReturnUrgent()
    {
        var priority = _service.SuggestPriority("مشکل فوری", "نیاز به رسیدگی سریع");

        priority.Should().Be(Ticket.TicketPriorities.Urgent);
    }

    [Fact]
    public void SuggestPriority_WithNormalMessage_ShouldReturnNormal()
    {
        var priority = _service.SuggestPriority("سوال درباره محصول", "می‌خواستم بدانم");

        priority.Should().Be(Ticket.TicketPriorities.Normal);
    }

    [Fact]
    public void FilterRequiringUrgentAttention_WithUrgentTickets_ShouldReturnThem()
    {
        var tickets = new List<Ticket>
        {
            new TicketBuilder().Build(),
            new TicketBuilder().AsUrgent().Build()
        };

        var urgent = _service.FilterRequiringUrgentAttention(tickets).ToList();

        urgent.Should().HaveCount(1);
        urgent.First().IsUrgent().Should().BeTrue();
    }
}