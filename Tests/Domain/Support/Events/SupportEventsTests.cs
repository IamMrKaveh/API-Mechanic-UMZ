using Domain.Support.Enums;
using Domain.Support.Events;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Support.Events;

public class SupportEventsTests
{
    [Fact]
    public void TicketCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var ticketId = TicketId.NewId();
        var customerId = UserId.NewId();

        var sut = new TicketCreatedEvent(
            ticketId, customerId, "Late delivery", "Shipping", TicketPriority.High);

        sut.TicketId.ShouldBe(ticketId);
        sut.CustomerId.ShouldBe(customerId);
        sut.Subject.ShouldBe("Late delivery");
        sut.Category.ShouldBe("Shipping");
        sut.Priority.ShouldBe(TicketPriority.High);
    }

    [Fact]
    public void TicketMessageAddedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var ticketId = TicketId.NewId();
        var messageId = TicketMessageId.NewId();
        var customerId = UserId.NewId();
        var senderId = UserId.NewId();

        var sut = new TicketMessageAddedEvent(
            ticketId, messageId, customerId, senderId, TicketMessageSenderType.Agent, 3);

        sut.TicketId.ShouldBe(ticketId);
        sut.MessageId.ShouldBe(messageId);
        sut.CustomerId.ShouldBe(customerId);
        sut.SenderId.ShouldBe(senderId);
        sut.SenderType.ShouldBe(TicketMessageSenderType.Agent);
        sut.NewMessageCount.ShouldBe(3);
    }

    [Fact]
    public void TicketStatusChangedEvent_ExposesPreviousAndNewStatus()
    {
        var sut = new TicketStatusChangedEvent(
            TicketId.NewId(), UserId.NewId(), TicketStatus.Open, TicketStatus.Answered);

        sut.PreviousStatus.ShouldBe(TicketStatus.Open);
        sut.NewStatus.ShouldBe(TicketStatus.Answered);
    }

    [Fact]
    public void TicketAnsweredEvent_ExposesTicketAndAdmin()
    {
        var ticketId = TicketId.NewId();
        var adminId = UserId.NewId();

        var sut = new TicketAnsweredEvent(ticketId, adminId);

        sut.TicketId.ShouldBe(ticketId);
        sut.AdminId.ShouldBe(adminId);
    }

    [Fact]
    public void TicketClosedEvent_ExposesTicketAndCustomer()
    {
        var ticketId = TicketId.NewId();
        var customerId = UserId.NewId();

        var sut = new TicketClosedEvent(ticketId, customerId);

        sut.TicketId.ShouldBe(ticketId);
        sut.CustomerId.ShouldBe(customerId);
    }
}
