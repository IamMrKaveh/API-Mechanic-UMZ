using Application.Wishlist.Features.Commands.ToggleWishlist;
using Mapster;
using Presentation.Wishlist.Mapping;
using Presentation.Wishlist.Requests;

namespace Tests.Presentation.Wishlist.Mapping;

public class WishlistMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly WishlistMappingConfig _sut = new();

    public WishlistMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void ToggleWishlistRequest_MapsToCommand()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ToggleWishlistRequest(productId);

        // Act
        var command = request.Adapt<ToggleWishlistCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ProductId.ShouldBe(request.ProductId);
    }

    [Fact]
    public void WishlistMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
