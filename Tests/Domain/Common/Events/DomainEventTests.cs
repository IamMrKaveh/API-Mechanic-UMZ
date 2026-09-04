using Domain.Common.Events;

namespace Tests.Domain.Common.Events;

public class DomainEventTests
{
    private sealed class TestEvent : DomainEvent
    {
        public TestEvent()
        {
        }

        public TestEvent(Guid correlationId)
            : base(correlationId)
        {
        }
    }

    [Fact]
    public void NewEvent_HasUniqueEventIdAndRecentOccurredAt()
    {
        var first = new TestEvent();
        var second = new TestEvent();

        first.EventId.ShouldNotBe(Guid.Empty);
        first.EventId.ShouldNotBe(second.EventId);
        first.OccurredAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        first.OccurredAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void NewEvent_HasFreshCorrelationIdAndNoCausation()
    {
        var sut = new TestEvent();

        sut.CorrelationId.ShouldNotBe(Guid.Empty);
        sut.CausationId.ShouldBeNull();
        sut.EventVersion.ShouldBe(1);
    }

    [Fact]
    public void Constructor_WithCorrelationId_SetsIt()
    {
        var correlationId = Guid.NewGuid();

        var sut = new TestEvent(correlationId);

        sut.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void WithCorrelationId_UpdatesAndReturnsSameInstance()
    {
        var sut = new TestEvent();
        var correlationId = Guid.NewGuid();

        var result = sut.WithCorrelationId(correlationId);

        result.ShouldBeSameAs(sut);
        sut.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void WithCausationId_UpdatesAndReturnsSameInstance()
    {
        var sut = new TestEvent();
        var causationId = Guid.NewGuid();

        var result = sut.WithCausationId(causationId);

        result.ShouldBeSameAs(sut);
        sut.CausationId.ShouldBe(causationId);
    }

    [Fact]
    public void SetCorrelation_UpdatesCorrelationId()
    {
        var sut = new TestEvent();
        var correlationId = Guid.NewGuid();

        sut.SetCorrelation(correlationId);

        sut.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void SetCausation_UpdatesCausationId()
    {
        var sut = new TestEvent();
        var causationId = Guid.NewGuid();

        sut.SetCausation(causationId);

        sut.CausationId.ShouldBe(causationId);
    }

    [Fact]
    public void ChainedCorrelationAndCausation_PreservesBoth()
    {
        var sut = new TestEvent();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        sut.WithCorrelationId(correlationId).WithCausationId(causationId);

        sut.CorrelationId.ShouldBe(correlationId);
        sut.CausationId.ShouldBe(causationId);
    }
}
