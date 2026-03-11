namespace Tests.DomainTest.Cart;

public class CartCreationTests
{
    [Fact]
    public void CreateForUser_WithValidUserId_ShouldCreateUserCart()
    {
        var cart = Cart.CreateForUser(1);

        cart.Should().NotBeNull();
        cart.UserId.Should().Be(1);
        cart.IsUserCart.Should().BeTrue();
        cart.IsGuestCart.Should().BeFalse();
    }

    [Fact]
    public void CreateForUser_WithZeroUserId_ShouldThrowException()
    {
        var act = () => Cart.CreateForUser(0);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateForGuest_WithValidToken_ShouldCreateGuestCart()
    {
        var cart = Cart.CreateForGuest("valid-guest-token");

        cart.IsGuestCart.Should().BeTrue();
        cart.IsUserCart.Should().BeFalse();
        cart.GuestToken.Should().Be("valid-guest-token");
    }

    [Fact]
    public void CreateForGuest_WithEmptyToken_ShouldThrowException()
    {
        var act = () => Cart.CreateForGuest("");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateForGuest_WithNoArguments_ShouldCreateGuestCartWithGeneratedToken()
    {
        var cart = Cart.CreateForGuest();

        cart.IsGuestCart.Should().BeTrue();
        cart.GuestToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_WithUserId_ShouldCreateUserCart()
    {
        var cart = Cart.Create(userId: 5, guestToken: null);

        cart.IsUserCart.Should().BeTrue();
        cart.UserId.Should().Be(5);
    }

    [Fact]
    public void Create_WithGuestToken_ShouldCreateGuestCart()
    {
        var cart = Cart.Create(userId: null, guestToken: "my-token");

        cart.IsGuestCart.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutUserOrGuest_ShouldThrowDomainException()
    {
        var act = () => Cart.Create(userId: null, guestToken: null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void NewCart_ShouldBeEmpty()
    {
        var cart = new CartBuilder().ForUser(1).Build();

        cart.IsEmpty.Should().BeTrue();
        cart.HasItems.Should().BeFalse();
        cart.TotalItems.Should().Be(0);
        cart.TotalPrice.Should().Be(0);
    }

    [Fact]
    public void Delete_ShouldMarkCartAsDeleted()
    {
        var cart = new CartBuilder().Build();

        cart.Delete(deletedBy: 1);

        cart.IsDeleted.Should().BeTrue();
        cart.DeletedBy.Should().Be(1);
        cart.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Restore_WhenDeleted_ShouldMarkAsNotDeleted()
    {
        var cart = new CartBuilder().Build();
        cart.Delete();

        cart.Restore();

        cart.IsDeleted.Should().BeFalse();
        cart.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void AssignToUser_WhenGuestCart_ShouldSetUserId()
    {
        var cart = Cart.CreateForGuest("token-123");

        cart.AssignToUser(7);

        cart.UserId.Should().Be(7);
        cart.IsUserCart.Should().BeTrue();
        cart.GuestToken.Should().BeNull();
    }

    [Fact]
    public void AssignToUser_WhenAlreadyUserCart_ShouldThrowException()
    {
        var cart = Cart.CreateForUser(1);

        var act = () => cart.AssignToUser(2);

        act.Should().Throw<Exception>();
    }
}