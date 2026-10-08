using Mapster;
using Presentation.Wallet.Mapping;

namespace Tests.Presentation.Wallet.Mapping;

public class WalletMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly WalletMappingConfig _sut = new();

    [Fact]
    public void Register_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() =>
        {
            _sut.Register(_config);
            _config.Compile();
        });

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void WalletMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
