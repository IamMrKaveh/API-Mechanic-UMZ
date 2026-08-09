using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Events;
using Tests.TestInfrastructure.Builders;
using WishlistAggregate = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Domain.Wishlist.Aggregates;

public class WishlistTests
{
    [Fact]
    public void Create_WithValidUserIdAndProductId_ReturnsInitializedWishlist()
    {
        var userId = UserId.NewId();
        var productId = ProductId.NewId();

        var sut = new WishlistBuilder()
            .WithUserId(userId)
            .WithProductId(productId)
            .Build();

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.UserId.ShouldBe(userId);
        sut.ProductId.ShouldBe(productId);
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WishlistBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_ProducesWishlistWithVersionOne()
    {
        new WishlistBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneWishlistItemAddedEvent()
    {
        var sut = new WishlistBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<WishlistItemAddedEvent>();
    }

    [Fact]
    public void Create_CalledTwice_ProducesDistinctIdentitiesAndInequality()
    {
        var userId = UserId.NewId();
        var productId = ProductId.NewId();

        var a = new WishlistBuilder().WithUserId(userId).WithProductId(productId).Build();
        var b = new WishlistBuilder().WithUserId(userId).WithProductId(productId).Build();

        a.Id.ShouldNotBe(b.Id);
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(
            () => WishlistAggregate.Create(null!, ProductId.NewId()));

        ex.ParamName.ShouldBe("userId");
    }

    [Fact]
    public void Create_WithNullProductId_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(
            () => WishlistAggregate.Create(UserId.NewId(), null!));

        ex.ParamName.ShouldBe("productId");
    }

    [Fact]
    public void ClearDomainEvents_AfterCreate_EmptiesTheDomainEventCollection()
    {
        var sut = new WishlistBuilder().Build();

        sut.ClearDomainEvents();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Equality_TwoWishlistsWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new WishlistBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoWishlistsWithDifferentIds_AreConsideredUnequal()
    {
        var a = new WishlistBuilder().Build();
        var b = new WishlistBuilder().Build();

        a.Equals(b).ShouldBeFalse();
    }
}
