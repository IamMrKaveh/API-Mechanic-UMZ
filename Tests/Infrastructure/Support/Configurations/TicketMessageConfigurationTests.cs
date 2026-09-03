using global::Domain.Support.Entities;
using global::Domain.Support.Enums;
using global::Domain.Support.ValueObjects;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Support.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class TicketMessageConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<global::Domain.Support.Aggregates.Ticket> SeedTicketAsync(CancellationToken ct = default)
    {
        var ticket = new TicketBuilder()
            .WithCustomerId(global::Domain.User.ValueObjects.UserId.NewId())
            .WithSubject($"Subject {Guid.NewGuid():N}"[..24])
            .Build();
        ticket.ClearDomainEvents();

        Context.Tickets.Add(ticket);
        await Context.SaveChangesAsync(ct);
        return ticket;
    }

    [Fact]
    public async Task SaveChanges_CustomerMessage_RoundTripsAllProperties()
    {
        var ticket = await SeedTicketAsync();
        var senderId = global::Domain.User.ValueObjects.UserId.NewId();
        var messageId = TicketMessageId.NewId();
        var sentAt = DateTime.UtcNow;

        var message = ticket.AddMessage(messageId, senderId, TicketMessageSenderType.Customer, "I have a billing question.", sentAt);
        ticket.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.TicketMessages.FirstOrDefaultAsync(m => m.Id == messageId);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(messageId);
        reloaded.TicketId.ShouldBe(ticket.Id);
        reloaded.SenderId.ShouldBe(senderId);
        reloaded.SenderType.ShouldBe(TicketMessageSenderType.Customer);
        reloaded.Content.ShouldBe("I have a billing question.");
        reloaded.IsEdited.ShouldBeFalse();
        reloaded.EditedAt.ShouldBeNull();
        reloaded.SentAt.ShouldBe(sentAt, TimeSpan.FromSeconds(1));
        reloaded.IsFromAgent().ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_AgentMessage_IsFromAgent()
    {
        var ticket = await SeedTicketAsync();
        var agentId = global::Domain.User.ValueObjects.UserId.NewId();
        var messageId = TicketMessageId.NewId();

        var message = ticket.AddMessage(messageId, agentId, TicketMessageSenderType.Agent, "We are looking into it.", DateTime.UtcNow);
        ticket.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.TicketMessages.FirstAsync(m => m.Id == messageId);
        reloaded.IsFromAgent().ShouldBeTrue();
        reloaded.SenderType.ShouldBe(TicketMessageSenderType.Agent);
    }

    [Fact]
    public async Task SaveChanges_MultipleMessagesForTicket_AllPersisted()
    {
        var ticket = await SeedTicketAsync();
        var senderId = global::Domain.User.ValueObjects.UserId.NewId();

        ticket.AddMessage(TicketMessageId.NewId(), senderId, TicketMessageSenderType.Customer, "First.", DateTime.UtcNow);
        ticket.AddMessage(TicketMessageId.NewId(), senderId, TicketMessageSenderType.Customer, "Second.", DateTime.UtcNow);
        ticket.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var count = await Context.TicketMessages.CountAsync(m => m.TicketId == ticket.Id);
        count.ShouldBe(2);
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(TicketMessage));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(TicketMessage.Id));
    }

    [Fact]
    public void Model_TicketIdAndSenderId_AreRequiredWithConverters()
    {
        var entityType = Context.Model.FindEntityType(typeof(TicketMessage));
        entityType.ShouldNotBeNull();

        var ticketId = entityType!.FindProperty(nameof(TicketMessage.TicketId));
        ticketId.ShouldNotBeNull();
        ticketId!.IsNullable.ShouldBeFalse();
        ticketId.GetValueConverter().ShouldNotBeNull();

        var senderId = entityType.FindProperty(nameof(TicketMessage.SenderId));
        senderId.ShouldNotBeNull();
        senderId!.IsNullable.ShouldBeFalse();
        senderId.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_Content_IsRequiredWithMaxLength5000()
    {
        var property = Context.Model.FindEntityType(typeof(TicketMessage))!.FindProperty(nameof(TicketMessage.Content));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(5000);
    }

    [Fact]
    public void Model_SenderType_IsRequiredStringWithMaxLength20()
    {
        var property = Context.Model.FindEntityType(typeof(TicketMessage))!.FindProperty(nameof(TicketMessage.SenderType));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(20);
    }

    [Fact]
    public void Model_IsEditedIsRequired_SentAtIsRequired_EditedAtIsOptional()
    {
        var entityType = Context.Model.FindEntityType(typeof(TicketMessage));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(TicketMessage.IsEdited))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(TicketMessage.SentAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(TicketMessage.EditedAt))!.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Model_HasIndexOnTicketId()
    {
        var entityType = Context.Model.FindEntityType(typeof(TicketMessage));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(TicketMessage.TicketId));
        index.ShouldNotBeNull();
    }
}
