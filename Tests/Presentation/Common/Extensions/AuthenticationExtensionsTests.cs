using System.Security.Claims;
using Application.Auth.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class AuthenticationExtensionsTests
{
    private const string Key = "0123456789abcdef0123456789abcdef";

    private static WebApplicationBuilder BuildBuilder(Dictionary<string, string?> values)
    {
        var builder = WebApplication.CreateBuilder();
        // Isolate from ambient appsettings.json so tests are deterministic.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        return builder;
    }

    private static Dictionary<string, string?> JwtConfig() => new()
    {
        ["Jwt:Key"] = Key,
        ["Jwt:Issuer"] = "test-issuer",
        ["Jwt:Audience"] = "test-audience"
    };

    [Fact]
    public void AddApplicationAuthentication_ConfiguresJwtBearerValidation()
    {
        var builder = BuildBuilder(JwtConfig());
        builder.AddApplicationAuthentication();

        var bearer = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        bearer.SaveToken.ShouldBeTrue();
        bearer.MapInboundClaims.ShouldBeTrue();
        bearer.TokenValidationParameters.ValidateIssuer.ShouldBeTrue();
        bearer.TokenValidationParameters.ValidIssuer.ShouldBe("test-issuer");
        bearer.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
        bearer.TokenValidationParameters.ValidAudience.ShouldBe("test-audience");
        bearer.TokenValidationParameters.ValidateIssuerSigningKey.ShouldBeTrue();
        bearer.TokenValidationParameters.ValidateLifetime.ShouldBeTrue();
        bearer.TokenValidationParameters.RequireExpirationTime.ShouldBeTrue();
        bearer.TokenValidationParameters.RequireSignedTokens.ShouldBeTrue();
        bearer.TokenValidationParameters.ValidAlgorithms.ShouldBe(["HS256"]);
        bearer.TokenValidationParameters.ClockSkew.ShouldBe(TimeSpan.FromSeconds(30));
        bearer.TokenValidationParameters.NameClaimType.ShouldBe(ClaimTypes.NameIdentifier);
        bearer.TokenValidationParameters.RoleClaimType.ShouldBe(ClaimTypes.Role);
    }

    [Fact]
    public void AddApplicationAuthentication_SetsDefaultSchemesToBearer()
    {
        var builder = BuildBuilder(JwtConfig());
        builder.AddApplicationAuthentication();

        var auth = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        auth.DefaultAuthenticateScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
        auth.DefaultChallengeScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public async Task AddApplicationAuthentication_WithoutGoogleConfig_DoesNotRegisterGoogleScheme()
    {
        var builder = BuildBuilder(JwtConfig());
        builder.AddApplicationAuthentication();

        var schemes = builder.Services.BuildServiceProvider()
            .GetRequiredService<IAuthenticationSchemeProvider>();

        (await schemes.GetSchemeAsync("Google")).ShouldBeNull();
    }

    [Fact]
    public async Task AddApplicationAuthentication_WithGoogleConfig_RegistersGoogleScheme()
    {
        var config = JwtConfig();
        config["Authentication:Google:ClientId"] = "client-id";
        config["Authentication:Google:ClientSecret"] = "client-secret";
        var builder = BuildBuilder(config);
        builder.AddApplicationAuthentication();

        var schemes = builder.Services.BuildServiceProvider()
            .GetRequiredService<IAuthenticationSchemeProvider>();

        (await schemes.GetSchemeAsync("Google")).ShouldNotBeNull();
    }

    [Fact]
    public async Task AddApplicationAuthentication_WithPartialGoogleConfig_DoesNotRegisterGoogleScheme()
    {
        var config = JwtConfig();
        config["Authentication:Google:ClientId"] = "client-id";
        var builder = BuildBuilder(config);
        builder.AddApplicationAuthentication();

        var schemes = builder.Services.BuildServiceProvider()
            .GetRequiredService<IAuthenticationSchemeProvider>();

        (await schemes.GetSchemeAsync("Google")).ShouldBeNull();
    }

    [Fact]
    public void AddApplicationAuthentication_ReturnsSameBuilder()
    {
        var builder = BuildBuilder(JwtConfig());

        builder.AddApplicationAuthentication().ShouldBeSameAs(builder);
    }
}
