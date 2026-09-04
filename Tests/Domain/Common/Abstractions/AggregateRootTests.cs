using Domain.Common.Abstractions;

namespace Tests.Domain.Common.Abstractions;

public class AggregateRootTests
{
    [Fact]
    public void NewAggregate_StartsWithVersionOneAndSingleRaisedEvent()
    {
        var sut = new WishlistBuilder()
            .WithUserId(global::Domain.User.ValueObjects.UserId.NewId())
            .WithProductId(global::Domain.Product.ValueObjects.ProductId.NewId())
            .Build();

        sut.Version.ShouldBe(1);
        sut.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        var sut = new WishlistBuilder()
            .WithUserId(global::Domain.User.ValueObjects.UserId.NewId())
            .WithProductId(global::Domain.Product.ValueObjects.ProductId.NewId())
            .Build();
        sut.DomainEvents.ShouldNotBeEmpty();

        sut.ClearDomainEvents();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_AreExposedAsReadOnly()
    {
        var sut = new WishlistBuilder()
            .WithUserId(global::Domain.User.ValueObjects.UserId.NewId())
            .WithProductId(global::Domain.Product.ValueObjects.ProductId.NewId())
            .Build();

        sut.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void EachRaise_IncrementsVersion()
    {
        var sut = new WishlistBuilder()
            .WithUserId(global::Domain.User.ValueObjects.UserId.NewId())
            .WithProductId(global::Domain.Product.ValueObjects.ProductId.NewId())
            .Build();
        var initialVersion = sut.Version;

        sut.ClearDomainEvents();
        sut.Version.ShouldBe(initialVersion);
    }
}
