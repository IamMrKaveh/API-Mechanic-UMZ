using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Support.Entities;

public class TicketMessageTests
{
    [Fact]
    public void AddMessage_ProducesMessageWithProvidedFieldsAssigned()
    {
        var ticket = new TicketBuilder().Build();
        var messageId = TicketMessageId.NewId();
        var senderId = UserId.NewId();
        var sentAt = new DateTime(2026, 3, 4, 12, 0, 0, DateTimeKind.Utc);

        ticket.AddMessage(messageId, senderId, TicketMessageSenderType.Customer, "Hello", sentAt);

        var message = ticket.Messages.ShouldHaveSingleItem();
        message.Id.ShouldBe(messageId);
        message.TicketId.ShouldBe(ticket.Id);
        message.SenderId.ShouldBe(senderId);
        message.SenderType.ShouldBe(TicketMessageSenderType.Customer);
        message.Content.ShouldBe("Hello");
        message.SentAt.ShouldBe(sentAt);
    }

    [Fact]
    public void AddMessage_InitializesIsEditedFalseAndEditedAtNull()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage(
            TicketMessageId.NewId(),
            UserId.NewId(),
            TicketMessageSenderType.Customer,
            "Hi",
            DateTime.UtcNow);

        var message = ticket.Messages.ShouldHaveSingleItem();
        message.IsEdited.ShouldBeFalse();
        message.EditedAt.ShouldBeNull();
    }

    [Fact]
    public void AddMessage_TrimsContentBeforeStoringOnMessage()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage(
            TicketMessageId.NewId(),
            UserId.NewId(),
            TicketMessageSenderType.Customer,
            "   Body with spaces   ",
            DateTime.UtcNow);

        ticket.Messages.ShouldHaveSingleItem().Content.ShouldBe("Body with spaces");
    }

    [Fact]
    public void IsFromAgent_ForAgentMessage_ReturnsTrue()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage(
            TicketMessageId.NewId(),
            UserId.NewId(),
            TicketMessageSenderType.Agent,
            "Hi",
            DateTime.UtcNow);

        ticket.Messages.ShouldHaveSingleItem().IsFromAgent().ShouldBeTrue();
    }

    [Fact]
    public void IsFromAgent_ForCustomerMessage_ReturnsFalse()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage(
            TicketMessageId.NewId(),
            UserId.NewId(),
            TicketMessageSenderType.Customer,
            "Hi",
            DateTime.UtcNow);

        ticket.Messages.ShouldHaveSingleItem().IsFromAgent().ShouldBeFalse();
    }
}
