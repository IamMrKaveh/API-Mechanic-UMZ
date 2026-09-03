using global::Domain.Support.Aggregates;
using global::Domain.Support.Enums;
using global::Domain.Support.ValueObjects;
using Tests.TestInfrastructure.Base;
using Tickets = global::Domain.Support.Aggregates.Ticket;

namespace Tests.Infrastructure.Support.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class TicketConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<Tickets> PersistAsync(Tickets ticket, CancellationToken ct = default)
    {
        ticket.ClearDomainEvents();
        Context.Tickets.Add(ticket);
        await Context.SaveChangesAsync(ct);
        return ticket;
    }

    [Fact]
    public async Task SaveChanges_OpenTicket_RoundTripsAllScalarProperties()
    {
        var customerId = global::Domain.User.ValueObjects.UserId.NewId();
        var ticket = new TicketBuilder()
            .WithCustomerId(customerId)
            .WithSubject("My order has not arrived")
            .WithCategoryValue("Shipping")
            .WithPriority(TicketPriority.High)
            .Build();
        await PersistAsync(ticket);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticket.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(ticket.Id);
        reloaded.CustomerId.ShouldBe(customerId);
        reloaded.AssignedAgentId.ShouldBeNull();
        reloaded.Subject.ShouldBe("My order has not arrived");
        reloaded.Status.Value.ShouldBe(TicketStatus.Open.Value);
        reloaded.Priority.Value.ShouldBe(TicketPriority.High.Value);
        reloaded.Category.Value.ShouldBe("Shipping");
        reloaded.ResolvedAt.ShouldBeNull();
        reloaded.MessageCount.ShouldBe(0);
        reloaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_DefaultPriority_IsNormal()
    {
        var ticket = new TicketBuilder().Build();
        await PersistAsync(ticket);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Tickets.FirstAsync(t => t.Id == ticket.Id);
        reloaded.Priority.Value.ShouldBe(TicketPriority.Normal.Value);
    }

    [Fact]
    public async Task SaveChanges_AddMessages_PersistsMessagesAndUpdatesActivity()
    {
        var customerId = global::Domain.User.ValueObjects.UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();
        await PersistAsync(ticket);

        var customerMessage = ticket.AddMessage(
            TicketMessageId.NewId(), customerId, TicketMessageSenderType.Customer, "Hello, I need help.", DateTime.UtcNow);
        ticket.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Tickets
            .Include(t => t.Messages)
            .FirstAsync(t => t.Id == ticket.Id);

        reloaded.Messages.Count.ShouldBe(1);
        reloaded.Messages[0].Content.ShouldBe("Hello, I need help.");
        reloaded.Messages[0].SenderType.ShouldBe(TicketMessageSenderType.Customer);
        reloaded.LastActivityAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_CloseTicket_PersistsClosedStatus()
    {
        var ticket = new TicketBuilder().Build();
        await PersistAsync(ticket);

        ticket.Close();
        ticket.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Tickets.FirstAsync(t => t.Id == ticket.Id);
        reloaded.Status.Value.ShouldBe(TicketStatus.Closed.Value);
        reloaded.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_WhenTicketIsDeleted_MessagesAreCascadeDeleted()
    {
        var customerId = global::Domain.User.ValueObjects.UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();
        await PersistAsync(ticket);

        var message = ticket.AddMessage(
            TicketMessageId.NewId(), customerId, TicketMessageSenderType.Customer, "Cascade me.", DateTime.UtcNow);
        ticket.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.Tickets
            .Include(t => t.Messages)
            .FirstAsync(t => t.Id == ticket.Id);
        Context.Tickets.Remove(loaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remaining = await Context.TicketMessages.CountAsync(m => m.TicketId == ticket.Id);
        remaining.ShouldBe(0);
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Tickets));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Tickets.Id));
    }

    [Fact]
    public void Model_Subject_IsRequiredWithMaxLength500()
    {
        var property = Context.Model.FindEntityType(typeof(Tickets))!.FindProperty(nameof(Tickets.Subject));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void Model_StatusPriorityAndCategory_HaveExpectedMaxLengthsAndConverters()
    {
        var entityType = Context.Model.FindEntityType(typeof(Tickets));
        entityType.ShouldNotBeNull();

        var status = entityType!.FindProperty(nameof(Tickets.Status));
        status.ShouldNotBeNull();
        status!.IsNullable.ShouldBeFalse();
        status.GetMaxLength().ShouldBe(50);
        status.GetValueConverter().ShouldNotBeNull();

        var priority = entityType.FindProperty(nameof(Tickets.Priority));
        priority.ShouldNotBeNull();
        priority!.IsNullable.ShouldBeFalse();
        priority.GetMaxLength().ShouldBe(50);
        priority.GetValueConverter().ShouldNotBeNull();

        var category = entityType.FindProperty(nameof(Tickets.Category));
        category.ShouldNotBeNull();
        category!.IsNullable.ShouldBeFalse();
        category.GetMaxLength().ShouldBe(100);
        category.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_CustomerIdIsRequired_AssignedAgentIdIsOptional()
    {
        var entityType = Context.Model.FindEntityType(typeof(Tickets));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Tickets.CustomerId))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Tickets.AssignedAgentId))!.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Model_HasExpectedIndexes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Tickets));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(Tickets.CustomerId), nameof(Tickets.AssignedAgentId), nameof(Tickets.Status) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
        }

        var composite = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == nameof(Tickets.Status))
                && i.Properties.Any(p => p.Name == nameof(Tickets.Priority)));
        composite.ShouldNotBeNull("composite index on Status+Priority should exist");
    }

    [Fact]
    public void Model_HasCascadeDeleteFromTicketToMessages()
    {
        var navigation = Context.Model.FindEntityType(typeof(Tickets))!.FindNavigation(nameof(Tickets.Messages));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        navigation.ForeignKey.Properties.Select(p => p.Name).ShouldBe([nameof(global::Domain.Support.Entities.TicketMessage.TicketId)]);
    }

    [Fact]
    public void Model_CreatedAtAndUpdatedAtAreRequired_OptionalDatesAreNullable()
    {
        var entityType = Context.Model.FindEntityType(typeof(Tickets));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Tickets.CreatedAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Tickets.UpdatedAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Tickets.LastActivityAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Tickets.ResolvedAt))!.IsNullable.ShouldBeTrue();
    }
}
