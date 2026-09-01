using Domain.Support.Enums;
using Domain.Support.Events;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;

namespace Tests.Domain.Support.Aggregates;

public class TicketTests
{
    [Fact]
    public void Open_WithValidInputs_ReturnsInitializedTicket()
    {
        var id = TicketId.NewId();
        var customerId = UserId.NewId();
        var category = new TicketCategoryBuilder().WithValue("Billing").Build();

        var ticket = new TicketBuilder()
            .WithId(id)
            .WithCustomerId(customerId)
            .WithSubject("Cannot pay")
            .WithCategory(category)
            .WithPriority(null)
            .Build();

        ticket.ShouldNotBeNull();
        ticket.Id.ShouldBe(id);
        ticket.CustomerId.ShouldBe(customerId);
        ticket.Subject.ShouldBe("Cannot pay");
        ticket.Category.ShouldBe(category);
        ticket.Priority.ShouldBe(TicketPriority.Normal);
        ticket.Status.ShouldBe(TicketStatus.Open);
        ticket.MessageCount.ShouldBe(0);
        ticket.Messages.ShouldBeEmpty();
        ticket.ResolvedAt.ShouldBeNull();
        ticket.AssignedAgentId.ShouldBeNull();
    }

    [Fact]
    public void Open_SetsCreatedAtAndLastActivityAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var ticket = new TicketBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        ticket.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        ticket.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        ticket.LastActivityAt!.ShouldBeGreaterThanOrEqualTo(before);
        ticket.LastActivityAt.ShouldBeLessThanOrEqualTo(after);
        ticket.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        ticket.UpdatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Open_TrimsSubjectBeforeStoring()
    {
        var ticket = new TicketBuilder().WithSubject("   Refund request   ").Build();

        ticket.Subject.ShouldBe("Refund request");
    }

    [Fact]
    public void Open_WithExplicitPriority_UsesThatPriority()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Urgent).Build();

        ticket.Priority.ShouldBe(TicketPriority.Urgent);
    }

    [Fact]
    public void Open_WithNullPriority_DefaultsToNormal()
    {
        var ticket = new TicketBuilder().WithPriority(null).Build();

        ticket.Priority.ShouldBe(TicketPriority.Normal);
    }

    [Fact]
    public void Open_WithNullId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new TicketBuilder().WithId(null!).Build());
    }

    [Fact]
    public void Open_WithNullCustomerId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new TicketBuilder().WithCustomerId(null!).Build());
    }

    [Fact]
    public void Open_WithNullCategory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new TicketBuilder().WithCategory(null!).Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_WithNullOrWhitespaceSubject_ThrowsArgumentException(string? subject)
    {
        Should.Throw<ArgumentException>(() => new TicketBuilder().WithSubject(subject!).Build());
    }

    [Fact]
    public void Open_QueryHelpers_ReflectOpenStatus()
    {
        var ticket = new TicketBuilder().Build();

        ticket.IsOpen.ShouldBeTrue();
        ticket.IsClosed.ShouldBeFalse();
        ticket.IsAwaitingReply.ShouldBeFalse();
        ticket.IsAnswered.ShouldBeFalse();
    }

    [Fact]
    public void Open_RaisesExactlyOneTicketCreatedEventWithCorrectPayload()
    {
        var id = TicketId.NewId();
        var customerId = UserId.NewId();
        var category = new TicketCategoryBuilder().WithValue("Billing").Build();

        var ticket = new TicketBuilder()
            .WithId(id)
            .WithCustomerId(customerId)
            .WithSubject("   Refund request   ")
            .WithCategory(category)
            .WithPriority(TicketPriority.High)
            .Build();

        ticket.DomainEvents.Count.ShouldBe(1);
        var evt = ticket.DomainEvents.OfType<TicketCreatedEvent>().ShouldHaveSingleItem();
        evt.TicketId.ShouldBe(id);
        evt.CustomerId.ShouldBe(customerId);
        evt.Subject.ShouldBe("Refund request");
        evt.Category.ShouldBe("Billing");
        evt.Priority.ShouldBe(TicketPriority.High);
    }

    [Fact]
    public void Open_ProducesTicketWithVersionOne()
    {
        new TicketBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Open_ProducesTicketImplementingIAuditable()
    {
        new TicketBuilder().Build().ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void AddMessage_FromCustomerOnOpenTicket_AppendsMessageAndKeepsStatusOpen()
    {
        var ticket = new TicketBuilder().Build();
        var now = DateTime.UtcNow;

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", now);

        ticket.MessageCount.ShouldBe(1);
        ticket.Status.ShouldBe(TicketStatus.Open);
    }

    [Fact]
    public void AddMessage_UpdatesLastActivityAtAndUpdatedAtToProvidedNow()
    {
        var ticket = new TicketBuilder().Build();
        var now = DateTime.UtcNow.AddMinutes(5);

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", now);

        ticket.LastActivityAt.ShouldBe(now);
        ticket.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void AddMessage_FromAgentOnOpenTicket_TransitionsStatusToAwaitingReply()
    {
        var ticket = new TicketBuilder().Build();

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "Hello", DateTime.UtcNow);

        ticket.Status.ShouldBe(TicketStatus.AwaitingReply);
        ticket.IsAwaitingReply.ShouldBeTrue();
    }

    [Fact]
    public void AddMessage_FromCustomerOnAwaitingReplyTicket_TransitionsStatusBackToOpen()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "Hello", DateTime.UtcNow);
        ticket.Status.ShouldBe(TicketStatus.AwaitingReply);

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Reply", DateTime.UtcNow);

        ticket.Status.ShouldBe(TicketStatus.Open);
        ticket.IsOpen.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddMessage_WithNullOrWhitespaceContent_ThrowsArgumentException(string? content)
    {
        var ticket = new TicketBuilder().Build();

        Should.Throw<ArgumentException>(() => ticket.AddMessage(
            TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, content!, DateTime.UtcNow));
    }

    [Fact]
    public void AddMessage_OnClosedTicket_ThrowsDomainException()
    {
        var ticket = new TicketBuilder().Build();
        ticket.Close();

        Should.Throw<DomainException>(() => ticket.AddMessage(
            TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", DateTime.UtcNow));
    }

    [Fact]
    public void AddMessage_OnClosedTicket_DoesNotAppendAndDoesNotRaiseEvent()
    {
        var ticket = new TicketBuilder().Build();
        ticket.Close();
        var messageCountBefore = ticket.MessageCount;
        var eventCountBefore = ticket.DomainEvents.Count;

        Should.Throw<DomainException>(() => ticket.AddMessage(
            TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", DateTime.UtcNow));

        ticket.MessageCount.ShouldBe(messageCountBefore);
        ticket.DomainEvents.Count.ShouldBe(eventCountBefore);
    }

    [Fact]
    public void AddMessage_RaisesTicketMessageAddedEventWithCorrectPayload()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();
        var messageId = TicketMessageId.NewId();
        var senderId = UserId.NewId();

        ticket.AddMessage(messageId, senderId, TicketMessageSenderType.Customer, "Hi", DateTime.UtcNow);

        var evt = ticket.DomainEvents.OfType<TicketMessageAddedEvent>().ShouldHaveSingleItem();
        evt.TicketId.ShouldBe(ticket.Id);
        evt.MessageId.ShouldBe(messageId);
        evt.CustomerId.ShouldBe(ticket.CustomerId);
        evt.SenderId.ShouldBe(senderId);
        evt.SenderType.ShouldBe(TicketMessageSenderType.Customer);
        evt.NewMessageCount.ShouldBe(1);
    }

    [Fact]
    public void AddMessage_TwoSequentialCalls_EachEventCarriesIncrementingNewMessageCount()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "One", DateTime.UtcNow);
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "Two", DateTime.UtcNow);

        var events = ticket.DomainEvents.OfType<TicketMessageAddedEvent>().ToList();
        events.Count.ShouldBe(2);
        events[0].NewMessageCount.ShouldBe(1);
        events[1].NewMessageCount.ShouldBe(2);
    }

    [Fact]
    public void AddMessage_IncrementsVersionByOnePerRaisedEvent()
    {
        var ticket = new TicketBuilder().Build();
        var versionBefore = ticket.Version;

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", DateTime.UtcNow);

        ticket.Version.ShouldBe(versionBefore + 1);
    }

    [Fact]
    public void Close_OnOpenTicket_SetsStatusToClosedAndUpdatesUpdatedAt()
    {
        var ticket = new TicketBuilder().Build();
        var updatedAtBefore = ticket.UpdatedAt;

        ticket.Close();

        ticket.Status.ShouldBe(TicketStatus.Closed);
        ticket.IsClosed.ShouldBeTrue();
        ticket.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    [Fact]
    public void Close_OnOpenTicket_RaisesTicketClosedEventWithCorrectPayload()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.Close();

        var evt = ticket.DomainEvents.OfType<TicketClosedEvent>().ShouldHaveSingleItem();
        evt.TicketId.ShouldBe(ticket.Id);
        evt.CustomerId.ShouldBe(ticket.CustomerId);
    }

    [Fact]
    public void Close_OnOpenTicket_RaisesTicketStatusChangedEventFromOpenToClosed()
    {
        var ticket = new TicketBuilder().Build();
        ticket.ClearDomainEvents();

        ticket.Close();

        var evt = ticket.DomainEvents.OfType<TicketStatusChangedEvent>().ShouldHaveSingleItem();
        evt.TicketId.ShouldBe(ticket.Id);
        evt.CustomerId.ShouldBe(ticket.CustomerId);
        evt.PreviousStatus.ShouldBe(TicketStatus.Open);
        evt.NewStatus.ShouldBe(TicketStatus.Closed);
    }

    [Fact]
    public void Close_FromAwaitingReply_RaisesStatusChangedEventWithAwaitingReplyAsPrevious()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "Hello", DateTime.UtcNow);
        ticket.ClearDomainEvents();

        ticket.Close();

        var evt = ticket.DomainEvents.OfType<TicketStatusChangedEvent>().ShouldHaveSingleItem();
        evt.PreviousStatus.ShouldBe(TicketStatus.AwaitingReply);
        evt.NewStatus.ShouldBe(TicketStatus.Closed);
    }

    [Fact]
    public void Close_OnAlreadyClosedTicket_DoesNotRaiseAdditionalEvents()
    {
        var ticket = new TicketBuilder().Build();
        ticket.Close();
        var eventCountAfterFirstClose = ticket.DomainEvents.Count;

        ticket.Close();

        ticket.DomainEvents.Count.ShouldBe(eventCountAfterFirstClose);
        ticket.Status.ShouldBe(TicketStatus.Closed);
    }

    [Theory]
    [InlineData("Low", false)]
    [InlineData("Normal", false)]
    [InlineData("High", true)]
    [InlineData("Urgent", true)]
    public void IsHighPriority_ReturnsExpectedForEachPriority(string priorityValue, bool expected)
    {
        var priority = TicketPriority.FromString(priorityValue);
        var ticket = new TicketBuilder().WithPriority(priority).Build();

        ticket.IsHighPriority().ShouldBe(expected);
    }

    [Theory]
    [InlineData("Low", false)]
    [InlineData("Normal", false)]
    [InlineData("High", false)]
    [InlineData("Urgent", true)]
    public void IsUrgent_ReturnsTrueOnlyForUrgentPriority(string priorityValue, bool expected)
    {
        var priority = TicketPriority.FromString(priorityValue);
        var ticket = new TicketBuilder().WithPriority(priority).Build();

        ticket.IsUrgent().ShouldBe(expected);
    }

    [Fact]
    public void RequiresUrgentAttention_WhenOpenAndUrgentAndOlderThanOneHour_ReturnsTrue()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Urgent).Build();
        var now = ticket.CreatedAt.AddHours(1.5);

        ticket.RequiresUrgentAttention(now).ShouldBeTrue();
    }

    [Fact]
    public void RequiresUrgentAttention_WhenOpenAndUrgentButYoungerThanOneHour_ReturnsFalse()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Urgent).Build();
        var now = ticket.CreatedAt.AddMinutes(30);

        ticket.RequiresUrgentAttention(now).ShouldBeFalse();
    }

    [Fact]
    public void RequiresUrgentAttention_WhenExactlyOneHourElapsed_ReturnsFalse()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Urgent).Build();
        var now = ticket.CreatedAt.AddHours(1);

        ticket.RequiresUrgentAttention(now).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Normal")]
    [InlineData("High")]
    public void RequiresUrgentAttention_WhenPriorityIsNotUrgent_ReturnsFalseRegardlessOfAge(string priorityValue)
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.FromString(priorityValue)).Build();
        var now = ticket.CreatedAt.AddHours(10);

        ticket.RequiresUrgentAttention(now).ShouldBeFalse();
    }

    [Fact]
    public void RequiresUrgentAttention_WhenTicketIsClosed_ReturnsFalseEvenWhenUrgentAndOld()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Urgent).Build();
        var oldNow = ticket.CreatedAt.AddHours(10);
        ticket.Close();

        ticket.RequiresUrgentAttention(oldNow).ShouldBeFalse();
    }

    [Fact]
    public void GetTimeToFirstResponse_WithNoMessages_ReturnsNull()
    {
        var ticket = new TicketBuilder().Build();

        ticket.GetTimeToFirstResponse().ShouldBeNull();
    }

    [Fact]
    public void GetTimeToFirstResponse_WithOnlyCustomerMessage_ReturnsNull()
    {
        var ticket = new TicketBuilder().Build();
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", DateTime.UtcNow);

        ticket.GetTimeToFirstResponse().ShouldBeNull();
    }

    [Fact]
    public void GetTimeToFirstResponse_WhenAgentReplyPrecedesCustomerMessage_ReturnsNull()
    {
        var ticket = new TicketBuilder().Build();
        var earlier = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var later = earlier.AddMinutes(30);

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "Prompt", earlier);
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", later);

        ticket.GetTimeToFirstResponse().ShouldBeNull();
    }

    [Fact]
    public void GetTimeToFirstResponse_WhenCustomerAsksThenAgentReplies_ReturnsElapsedBetweenThem()
    {
        var ticket = new TicketBuilder().Build();
        var customerAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var agentAt = customerAt.AddMinutes(45);

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "Hi", customerAt);
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "Hello", agentAt);

        ticket.GetTimeToFirstResponse().ShouldBe(TimeSpan.FromMinutes(45));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        var ticket = new TicketBuilder().Build();

        ticket.DomainEvents.Count.ShouldBe(1);
        ticket.ClearDomainEvents();
        ticket.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void LifecycleSequence_OpenCustomerAgentCustomerClose_AccumulatesEventsInOrder()
    {
        var ticket = new TicketBuilder().Build();
        var t = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "one", t);
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Agent, "two", t.AddMinutes(1));
        ticket.AddMessage(TicketMessageId.NewId(), UserId.NewId(), TicketMessageSenderType.Customer, "three", t.AddMinutes(2));
        ticket.Close();

        ticket.DomainEvents.Count.ShouldBe(6);
        ticket.DomainEvents.ElementAt(0).ShouldBeOfType<TicketCreatedEvent>();
        ticket.DomainEvents.ElementAt(1).ShouldBeOfType<TicketMessageAddedEvent>();
        ticket.DomainEvents.ElementAt(2).ShouldBeOfType<TicketMessageAddedEvent>();
        ticket.DomainEvents.ElementAt(3).ShouldBeOfType<TicketMessageAddedEvent>();
        ticket.DomainEvents.ElementAt(4).ShouldBeOfType<TicketClosedEvent>();
        ticket.DomainEvents.ElementAt(5).ShouldBeOfType<TicketStatusChangedEvent>();
    }
}
