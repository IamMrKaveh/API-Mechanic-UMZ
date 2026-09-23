using Application.Wishlist.Mapping;
using Mapster;

namespace Tests.Application.Wishlist.Mapping;

public class WishlistMappingConfigTests
{
    [Fact]
    public void Register_OnEmptyConfig_DoesNotThrow()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new WishlistMappingConfig().Register(config));
    }

    [Fact]
    public void Register_ProducesCompilableConfig()
    {
        var config = new TypeAdapterConfig();
        new WishlistMappingConfig().Register(config);

        Should.NotThrow(() => config.Compile());
    }

    [Fact]
    public void WishlistMappingConfig_ImplementsIRegister()
    {
        new WishlistMappingConfig().ShouldBeAssignableTo<IRegister>();
    }
}
