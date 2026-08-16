using Application.Common.Events;
using Domain.Common.Abstractions;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence;

namespace Tests.Infrastructure.Persistence;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WithEmptyEnumerable_DoesNotInvokePublisher()
    {
        var publisher = Substitute.For<IPublisher>(); var sut = new DomainEventDispatcher(publisher);

        await sut.DispatchAsync([]);

        await publisher.DidNotReceiveWithAnyArgs().Publish(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithSingleEvent_PublishesTypedDomainEventNotification()
    {
        var publisher = Substitute.For<IPublisher>();
        var sut = new DomainEventDispatcher(publisher);
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());

        await sut.DispatchAsync([domainEvent]);

        await publisher.Received(1).Publish(
            Arg.Is<object>(o =>
                o is DomainEventNotification<OrderExpiredEvent> &&
                ((DomainEventNotification<OrderExpiredEvent>)o).DomainEvent == domainEvent),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleEvents_PublishesEachEventInOrder()
    {
        var publisher = Substitute.For<IPublisher>();
        var sut = new DomainEventDispatcher(publisher);
        var first = new OrderExpiredEvent(OrderId.NewId());
        var second = new OrderExpiredEvent(OrderId.NewId());

        await sut.DispatchAsync([first, second]);

        Received.InOrder(() =>
        {
            publisher.Publish(
                Arg.Is<object>(o =>
                    o is DomainEventNotification<OrderExpiredEvent> &&
                    ((DomainEventNotification<OrderExpiredEvent>)o).DomainEvent == first),
                Arg.Any<CancellationToken>());
            publisher.Publish(
                Arg.Is<object>(o =>
                    o is DomainEventNotification<OrderExpiredEvent> &&
                    ((DomainEventNotification<OrderExpiredEvent>)o).DomainEvent == second),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task DispatchAsync_ForwardsCancellationTokenToPublisher()
    {
        var publisher = Substitute.For<IPublisher>();
        var sut = new DomainEventDispatcher(publisher);
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        using var cts = new CancellationTokenSource();

        await sut.DispatchAsync(new IDomainEvent[] { domainEvent }, cts.Token);

        await publisher.Received(1).Publish(Arg.Any<object>(), cts.Token);
    }
}
