namespace Tests.DomainTest.Support;

public class TicketQueryMethodTests
{
    [Fact]
    public void CanAddMessage_WhenOpen_ShouldReturnTrue()
    {
        var ticket = new TicketBuilder().Build();

        ticket.CanAddMessage().Should().BeTrue();
    }

    [Fact]
    public void CanAddMessage_WhenClosed_ShouldReturnFalse()
    {
        var ticket = new TicketBuilder().BuildClosed();

        ticket.CanAddMessage().Should().BeFalse();
    }

    [Fact]
    public void CanClose_WhenOpen_ShouldReturnTrue()
    {
        var ticket = new TicketBuilder().Build();

        ticket.CanClose().Should().BeTrue();
    }

    [Fact]
    public void CanClose_WhenAlreadyClosed_ShouldReturnFalse()
    {
        var ticket = new TicketBuilder().BuildClosed();

        ticket.CanClose().Should().BeFalse();
    }

    [Fact]
    public void CanReopen_WhenClosed_ShouldReturnTrue()
    {
        var ticket = new TicketBuilder().BuildClosed();

        ticket.CanReopen().Should().BeTrue();
    }

    [Fact]
    public void CanReopen_WhenOpen_ShouldReturnFalse()
    {
        var ticket = new TicketBuilder().Build();

        ticket.CanReopen().Should().BeFalse();
    }

    [Fact]
    public void HasAdminResponse_WithoutAdminMessage_ShouldReturnFalse()
    {
        var ticket = new TicketBuilder().Build();

        ticket.HasAdminResponse().Should().BeFalse();
    }

    [Fact]
    public void HasAdminResponse_WithAdminMessage_ShouldReturnTrue()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage("پاسخ ادمین", isAdminReply: true);

        ticket.HasAdminResponse().Should().BeTrue();
    }

    [Fact]
    public void GetLastAdminResponse_WithAdminMessage_ShouldReturnLastAdminMessage()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage("پاسخ اول ادمین", isAdminReply: true);
        ticket.AddMessage("پیام کاربر", isAdminReply: false);
        ticket.AddMessage("پاسخ دوم ادمین", isAdminReply: true);

        var lastAdmin = ticket.GetLastAdminResponse();

        lastAdmin.Should().NotBeNull();
        lastAdmin!.Content.Should().Be("پاسخ دوم ادمین");
    }

    [Fact]
    public void GetLastAdminResponse_WithoutAdminMessage_ShouldReturnNull()
    {
        var ticket = new TicketBuilder().Build();

        var result = ticket.GetLastAdminResponse();

        result.Should().BeNull();
    }

    [Fact]
    public void GetLastUserMessage_ShouldReturnLatestUserMessage()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage("پاسخ ادمین", isAdminReply: true);
        ticket.AddMessage("پیام دوم کاربر", isAdminReply: false);

        var lastUser = ticket.GetLastUserMessage();

        lastUser.Should().NotBeNull();
        lastUser!.Content.Should().Be("پیام دوم کاربر");
    }

    [Fact]
    public void LastMessageAt_ShouldReturnMostRecentMessageDate()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage("پیام دوم", isAdminReply: false);

        ticket.LastMessageAt.Should().NotBeNull();
        ticket.LastMessageAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MessageCount_ShouldReflectAllMessages()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage("پیام دوم", isAdminReply: false);
        ticket.AddMessage("پاسخ ادمین", isAdminReply: true);

        ticket.MessageCount.Should().Be(3);
    }

    [Fact]
    public void GetTimeSinceCreation_ShouldBePositive()
    {
        var ticket = new TicketBuilder().Build();

        var timeSince = ticket.GetTimeSinceCreation();

        timeSince.Should().NotBeNull();
        timeSince!.Value.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }
}