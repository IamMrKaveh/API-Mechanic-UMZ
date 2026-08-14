using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Support.Repositories;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using TicketAggregate = Domain.Support.Aggregates.Ticket;

namespace Tests.Infrastructure.Support.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class TicketRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private TicketRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new TicketRepository(_context);
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

    [RequiresDockerFact]
    public async Task AddAsync_WithValidTicket_PersistsToDatabase()
    {
        var customerId = UserId.NewId();
        var ticket = new TicketBuilder()
            .WithCustomerId(customerId)
            .WithSubject("Cannot complete my order")
            .WithCategoryValue("Billing")
            .WithPriority(TicketPriority.High)
            .Build();

        await _sut.AddAsync(ticket);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticket.Id);

        persisted.ShouldNotBeNull();
        persisted.CustomerId.ShouldBe(customerId);
        persisted.Subject.ShouldBe("Cannot complete my order");
        persisted.Category.ShouldBe(TicketCategory.Create("Billing"));
        persisted.Priority.ShouldBe(TicketPriority.High);
        persisted.Status.ShouldBe(TicketStatus.Open);
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_WhenTicketExists_ReturnsTicketWithoutMessages()
    {
        var persisted = await PersistTicketAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new TicketRepository(queryContext);

        var result = await sut.GetByIdAsync(persisted.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(persisted.Id);
        result.CustomerId.ShouldBe(persisted.CustomerId);
        result.Messages.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_WhenTicketDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(TicketId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetByIdWithMessagesAsync_WhenTicketExists_ReturnsTicketWithMessages()
    {
        var persisted = await PersistTicketAsync();
        persisted.AddMessage(
            TicketMessageId.NewId(),
            persisted.CustomerId,
            TicketMessageSenderType.Customer,
            "Initial message",
            DateTime.UtcNow);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new TicketRepository(queryContext);

        var result = await sut.GetByIdWithMessagesAsync(persisted.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(persisted.Id);
        result.Messages.Count.ShouldBe(1);
        result.Messages.Single().Content.ShouldBe("Initial message");
    }

    [RequiresDockerFact]
    public async Task GetByIdWithMessagesAsync_WhenTicketDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdWithMessagesAsync(TicketId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task Update_WithClosedTicket_PersistsStatusChange()
    {
        var persisted = await PersistTicketAsync();

        persisted.Close();
        _sut.Update(persisted);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var reloaded = await queryContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == persisted.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Status.ShouldBe(TicketStatus.Closed);
    }

    [RequiresDockerFact]
    public async Task Update_WithAddedMessage_PersistsMessageAndActivity()
    {
        var persisted = await PersistTicketAsync();
        var now = DateTime.UtcNow;

        persisted.AddMessage(
            TicketMessageId.NewId(),
            UserId.NewId(),
            TicketMessageSenderType.Agent,
            "Agent reply",
            now);
        _sut.Update(persisted);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var reloaded = await queryContext.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == persisted.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Status.ShouldBe(TicketStatus.AwaitingReply);
        reloaded.LastActivityAt.ShouldBe(now);
        reloaded.Messages.Count.ShouldBe(1);
        reloaded.Messages.Single().Content.ShouldBe("Agent reply");
        reloaded.Messages.Single().SenderType.ShouldBe(TicketMessageSenderType.Agent);
    }
}
