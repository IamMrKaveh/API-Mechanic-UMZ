using System.Reflection;
using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.Communication.Contracts;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Infrastructure.Auth.Services;
using Infrastructure.Security.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.Common.Extensions;

namespace Tests.Security;

/// <summary>
/// Phase 6 invariants (security audit) for password hashing, OTP generation and
/// token validation. These assert the primitives; the source-hygiene suite asserts
/// that the weak alternatives never reappear elsewhere.
/// </summary>
public class CryptoAndTokenInvariantsTests
{
    // -- Item 1: BCrypt password hashing with work factor 12 ------------------

    [Fact]
    public void PasswordHasher_KeepsBcryptWorkFactorAtTwelve()
    {
        var field = typeof(PasswordHasher).GetField(
            "WorkFactor", BindingFlags.NonPublic | BindingFlags.Static);

        field.ShouldNotBeNull("PasswordHasher must keep a WorkFactor constant");
        field!.GetRawConstantValue().ShouldBe(12);
    }

    [Fact]
    public void PasswordHasher_EmitsCostTwelveHashes()
    {
        var hash = new PasswordHasher().Hash("Str0ngP@ssw0rd!");

        // BCrypt format: $2a$12$<salt><hash>
        var parts = hash.Split('$');
        parts.Length.ShouldBe(4);
        parts[1].ShouldStartWith("2");
        parts[2].ShouldBe("12");
    }

    // -- Item 2: CSPRNG OTP generation, hashed storage ----------------------

    [Fact]
    public void OtpCode_Generate_ProducesSixDigitCodesWithHighEntropy()
    {
        var codes = Enumerable.Range(0, 200)
            .Select(_ => OtpCode.Generate().Value)
            .ToList();

        codes.ShouldAllBe(code => code.Length == 6 && code.All(char.IsDigit));
        codes.Distinct().Count().ShouldBeGreaterThan(
            150, "CSPRNG-backed generation must not repeat codes at this sample size");
    }

    [Fact]
    public void OtpCode_MatchesHash_RejectsWrongCodeAndMalformedHash()
    {
        var otp = OtpCode.Create("123456");
        var hash = otp.ToHash();

        otp.MatchesHash(hash).ShouldBeTrue();
        OtpCode.Create("654321").MatchesHash(hash).ShouldBeFalse();
        otp.MatchesHash("not-a-hash").ShouldBeFalse();
        otp.MatchesHash(string.Empty).ShouldBeFalse();
    }

    [Fact]
    public void OtpService_HashesWithServerPepperAndNeverStoresTheRawCode()
    {
        var service = new OtpService(
            Substitute.For<IOtpRepository>(),
            Substitute.For<ISmsService>(),
            Options.Create(new AuthOptions { OtpPepper = "phase-six-pepper-value" }),
            Substitute.For<IAuditService>());

        var hash = service.HashOtp(OtpCode.Create("123456"));

        hash.ShouldNotContain("123456");
        Convert.FromBase64String(hash).Length.ShouldBe(32); // HMAC-SHA256 output
    }

    [Fact]
    public void OtpService_WithMissingPepper_FailsFast()
    {
        var act = () => new OtpService(
            Substitute.For<IOtpRepository>(),
            Substitute.For<ISmsService>(),
            Options.Create(new AuthOptions { OtpPepper = string.Empty }),
            Substitute.For<IAuditService>());

        act.ShouldThrow<InvalidOperationException>();
    }

    // -- Item 7: strict JWT validation --------------------------------------

    [Fact]
    public void JwtValidation_PinsHmacSha256AndStrictLifetimeChecks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "invariant-issuer",
            ["Jwt:Audience"] = "invariant-audience"
        });
        builder.Services.Configure<JwtOptions>(
            builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.AddApplicationAuthentication();

        var bearer = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        var parameters = bearer.TokenValidationParameters;

        parameters.ValidAlgorithms.ShouldBe([SecurityAlgorithms.HmacSha256]);
        parameters.ValidateIssuer.ShouldBeTrue();
        parameters.ValidateAudience.ShouldBeTrue();
        parameters.ValidateIssuerSigningKey.ShouldBeTrue();
        parameters.ValidateLifetime.ShouldBeTrue();
        parameters.RequireExpirationTime.ShouldBeTrue();
        parameters.RequireSignedTokens.ShouldBeTrue();
        parameters.ClockSkew.ShouldBe(TimeSpan.FromSeconds(30));
    }
}
