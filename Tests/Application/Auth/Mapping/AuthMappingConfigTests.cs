using Application.Auth.Mapping;
using Mapster;

namespace Tests.Application.Auth.Mapping;

public class AuthMappingConfigTests
{
    [Fact]
    public void Register_OnEmptyConfig_DoesNotThrow()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new AuthMappingConfig().Register(config));
    }

    [Fact]
    public void Register_ProducesCompilableConfig()
    {
        var config = new TypeAdapterConfig();
        new AuthMappingConfig().Register(config);

        Should.NotThrow(() => config.Compile());
    }

    [Fact]
    public void AuthMappingConfig_ImplementsIRegister()
    {
        new AuthMappingConfig().ShouldBeAssignableTo<IRegister>();
    }
}
