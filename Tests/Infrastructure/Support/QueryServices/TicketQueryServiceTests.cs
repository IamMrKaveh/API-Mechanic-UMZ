using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Support.QueryServices;
using TicketAggregate = Domain.Support.Aggregates.Ticket;

namespace Tests.Infrastructure.Support.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class TicketQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private TicketQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new TicketQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<TicketAggregate> PersistTicketAsync(
        UserId? customerId = null,
        string? subject = null,
        string? category = null,
        TicketPriority? priority = null)
    {
        var builder = new TicketBuilder();

        if (customerId is not null)
            builder = builder.WithCustomerId(customerId);

        if (subject is not null)
            builder = builder.WithSubject(subject);

        if (category is not null)
            builder = builder.WithCategoryValue(category);

        if (priority is not null)
            builder = builder.WithPriority(priority);

        var ticket = builder.Build();
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    [Fact]
    public async Task GetAdminTicketsPagedAsync_WhenNoMatchingTickets_ReturnsEmptyResult()
    {
        await PersistTicketAsync(priority: TicketPriority.Low);

        var result = await _sut.GetAdminTicketsPagedAsync(
            TicketStatus.AwaitingReply,
            TicketPriority.Urgent,
            null,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetAdminTicketsPagedAsync_FiltersByStatusAndPriority()
    {
        var customerId = UserId.NewId();
        var matching = await PersistTicketAsync(customerId: customerId, subject: "Billing issue", category: "Billing", priority: TicketPriority.High);
        await PersistTicketAsync(customerId: customerId, subject: "Other issue", category: "Billing", priority: TicketPriority.Low);

        var result = await _sut.GetAdminTicketsPagedAsync(
            TicketStatus.Open,
            TicketPriority.High,
            null,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);

        var dto = result.Items.Single();
        dto.Id.ShouldBe(matching.Id.Value);
        dto.CustomerId.ShouldBe(customerId.Value);
        dto.AssignedAgentId.ShouldBeNull();
        dto.Subject.ShouldBe("Billing issue");
        dto.Category.ShouldBe("Billing");
        dto.Priority.ShouldBe(TicketPriority.High.Value);
        dto.PriorityDisplayName.ShouldBe(TicketPriority.High.DisplayName);
        dto.Status.ShouldBe(TicketStatus.Open.Value);
        dto.StatusDisplayName.ShouldBe(TicketStatus.Open.DisplayName);
        dto.MessageCount.ShouldBe(0);

        dto.CreatedAt.ShouldBe(matching.CreatedAt, TimeSpan.FromMicroseconds(1));
        dto.UpdatedAt.ShouldBe(matching.UpdatedAt, TimeSpan.FromMicroseconds(1));
        dto.LastActivityAt.ShouldBe(matching.LastActivityAt, TimeSpan.FromMicroseconds(1));
        dto.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetAdminTicketsPagedAsync_WithUserIdFilter_ReturnsOnlyThatCustomerTickets()
    {
        var customerId = UserId.NewId();
        var otherCustomerId = UserId.NewId();
        await PersistTicketAsync(customerId: customerId, priority: TicketPriority.High);
        await PersistTicketAsync(customerId: otherCustomerId, priority: TicketPriority.High);

        var result = await _sut.GetAdminTicketsPagedAsync(
            TicketStatus.Open,
            TicketPriority.High,
            customerId,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().CustomerId.ShouldBe(customerId.Value);
    }

    [Fact]
    public async Task GetAdminTicketsPagedAsync_OrderedByCreatedAtDescending()
    {
        var t1 = await PersistTicketAsync(priority: TicketPriority.Normal);
        await Task.Delay(5);
        var t2 = await PersistTicketAsync(priority: TicketPriority.Normal);
        await Task.Delay(5);
        var t3 = await PersistTicketAsync(priority: TicketPriority.Normal);

        var result = await _sut.GetAdminTicketsPagedAsync(
            TicketStatus.Open,
            TicketPriority.Normal,
            null,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        var ids = result.Items.Select(i => i.Id).ToList();
        ids[0].ShouldBe(t3.Id.Value);
        ids[1].ShouldBe(t2.Id.Value);
        ids[2].ShouldBe(t1.Id.Value);
    }

    [Fact]
    public async Task GetAdminTicketsPagedAsync_WithPaging_ReturnsCorrectPage()
    {
        await PersistTicketAsync(priority: TicketPriority.Normal);
        await Task.Delay(5);
        await PersistTicketAsync(priority: TicketPriority.Normal);
        await Task.Delay(5);
        await PersistTicketAsync(priority: TicketPriority.Normal);

        var page1 = await _sut.GetAdminTicketsPagedAsync(TicketStatus.Open, TicketPriority.Normal, null, page: 1, pageSize: 2, CancellationToken.None);
        var page2 = await _sut.GetAdminTicketsPagedAsync(TicketStatus.Open, TicketPriority.Normal, null, page: 2, pageSize: 2, CancellationToken.None);

        page1.TotalCount.ShouldBe(3);
        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -5)]
    public async Task GetAdminTicketsPagedAsync_WithInvalidPaging_UsesDefaults(int page, int pageSize)
    {
        await PersistTicketAsync(priority: TicketPriority.Normal);

        var result = await _sut.GetAdminTicketsPagedAsync(
            TicketStatus.Open,
            TicketPriority.Normal,
            null,
            page,
            pageSize,
            CancellationToken.None);

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetTicketDetailAsync_WhenTicketExists_ReturnsDetailWithOrderedMessages()
    {
        var customerId = UserId.NewId();
        var agentId = UserId.NewId();
        var ticket = await PersistTicketAsync(customerId: customerId, subject: "Payment failed", category: "Billing", priority: TicketPriority.Urgent);

        var baseTime = DateTime.UtcNow;
        ticket.AddMessage(TicketMessageId.NewId(), customerId, TicketMessageSenderType.Customer, "First message", baseTime);
        ticket.AddMessage(TicketMessageId.NewId(), agentId, TicketMessageSenderType.Agent, "Second message", baseTime.AddMinutes(5));
        await _context.SaveChangesAsync();

        var result = await _sut.GetTicketDetailAsync(ticket.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(ticket.Id.Value);
        result.UserId.ShouldBe(customerId.Value);
        result.CustomerId.ShouldBe(customerId.Value);
        result.AssignedAgentId.ShouldBeNull();
        result.Subject.ShouldBe("Payment failed");
        result.Category.ShouldBe("Billing");
        result.Priority.ShouldBe(TicketPriority.Urgent.Value);
        result.Status.ShouldBe(TicketStatus.AwaitingReply.Value);
        result.MessageCount.ShouldBe(2);

        result.Messages.Count.ShouldBe(2);
        result.Messages[0].Content.ShouldBe("First message");
        result.Messages[0].SenderType.ShouldBe("Customer");
        result.Messages[0].IsAdminReply.ShouldBeFalse();
        result.Messages[0].SenderId.ShouldBe(customerId.Value);
        result.Messages[1].Content.ShouldBe("Second message");
        result.Messages[1].SenderType.ShouldBe("Agent");
        result.Messages[1].IsAdminReply.ShouldBeTrue();
        result.Messages[1].SenderId.ShouldBe(agentId.Value);
    }

    [Fact]
    public async Task GetTicketDetailAsync_WhenTicketDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetTicketDetailAsync(TicketId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WhenNoTickets_ReturnsEmptyResult()
    {
        var result = await _sut.GetTicketsPagedAsync(
            UserId.NewId(),
            null,
            null,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_ReturnsMappedListItems()
    {
        var customerId = UserId.NewId();
        var ticket = await PersistTicketAsync(customerId: customerId, subject: "Refund request", category: "Billing", priority: TicketPriority.Low);

        var result = await _sut.GetTicketsPagedAsync(
            customerId,
            null,
            null,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);

        var dto = result.Items.Single();
        dto.Id.ShouldBe(ticket.Id.Value);
        dto.Subject.ShouldBe("Refund request");
        dto.Category.ShouldBe("Billing");
        dto.Priority.ShouldBe(TicketPriority.Low.Value);
        dto.Status.ShouldBe(TicketStatus.Open.Value);
        dto.MessageCount.ShouldBe(0);

        dto.CreatedAt.ShouldBe(ticket.CreatedAt, TimeSpan.FromMicroseconds(1));
        dto.LastReplyAt.ShouldBe(ticket.LastActivityAt, TimeSpan.FromMicroseconds(1));
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        var customerId = UserId.NewId();
        var openTicket = await PersistTicketAsync(customerId: customerId);
        var closedTicket = await PersistTicketAsync(customerId: customerId);
        closedTicket.Close();
        await _context.SaveChangesAsync();

        var result = await _sut.GetTicketsPagedAsync(
            customerId,
            TicketStatus.Open,
            null,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(openTicket.Id.Value);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithPriorityFilter_ReturnsOnlyMatchingPriority()
    {
        var customerId = UserId.NewId();
        var highTicket = await PersistTicketAsync(customerId: customerId, priority: TicketPriority.High);
        await PersistTicketAsync(customerId: customerId, priority: TicketPriority.Low);

        var result = await _sut.GetTicketsPagedAsync(
            customerId,
            null,
            TicketPriority.High,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(highTicket.Id.Value);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_OrderedByCreatedAtDescending()
    {
        var customerId = UserId.NewId();
        var t1 = await PersistTicketAsync(customerId: customerId);
        await Task.Delay(5);
        var t2 = await PersistTicketAsync(customerId: customerId);
        await Task.Delay(5);
        var t3 = await PersistTicketAsync(customerId: customerId);

        var result = await _sut.GetTicketsPagedAsync(customerId, null, null, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        var ids = result.Items.Select(i => i.Id).ToList();
        ids[0].ShouldBe(t3.Id.Value);
        ids[1].ShouldBe(t2.Id.Value);
        ids[2].ShouldBe(t1.Id.Value);
    }

    [Fact]
    public async Task GetTicketsPagedAsync_WithPaging_ReturnsCorrectPage()
    {
        var customerId = UserId.NewId();
        await PersistTicketAsync(customerId: customerId);
        await Task.Delay(5);
        await PersistTicketAsync(customerId: customerId);
        await Task.Delay(5);
        await PersistTicketAsync(customerId: customerId);

        var page1 = await _sut.GetTicketsPagedAsync(customerId, null, null, page: 1, pageSize: 2, CancellationToken.None);
        var page2 = await _sut.GetTicketsPagedAsync(customerId, null, null, page: 2, pageSize: 2, CancellationToken.None);

        page1.TotalCount.ShouldBe(3);
        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -5)]
    public async Task GetTicketsPagedAsync_WithInvalidPaging_UsesDefaults(int page, int pageSize)
    {
        var customerId = UserId.NewId();
        await PersistTicketAsync(customerId: customerId);

        var result = await _sut.GetTicketsPagedAsync(customerId, null, null, page, pageSize, CancellationToken.None);

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
        result.TotalCount.ShouldBe(1);
    }
}
