using Application.Auth.Features.Commands.RefreshToken;

namespace Tests.Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenValidatorTests
{
    private readonly RefreshTokenValidator _sut = new();

    [Fact]
    public void Validate_WithNonEmptyRefreshToken_IsValid()
    {
        var command = new RefreshTokenCommand("some-refresh-token-value");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceRefreshToken_FailsOnRefreshToken(string refreshToken)
    {
        var command = new RefreshTokenCommand(refreshToken);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }
}
