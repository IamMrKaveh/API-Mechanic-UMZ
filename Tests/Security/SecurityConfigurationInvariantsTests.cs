using System.Text.Json;

namespace Tests.Security;

/// <summary>
/// Phase 6 invariants (security audit) for the configuration that is committed to
/// the repository. Each assertion maps to a strength that must not regress:
/// secrets stay out of appsettings, allow-lists stay explicit, headers stay on.
/// See docs/07-operations/security-invariants.md.
/// </summary>
public class SecurityConfigurationInvariantsTests
{
    private static readonly JsonElement Settings = SecurityInvariantHelpers.ReadAppSettings();

    [Fact]
    public void AllowedHosts_IsConfiguredAndNotWildcard()
    {
        var allowedHosts = SecurityInvariantHelpers.GetString(Settings, "AllowedHosts");

        allowedHosts.ShouldNotBeNullOrWhiteSpace();
        allowedHosts!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ShouldNotContain("*");
    }

    [Fact]
    public void CommittedAppSettings_ContainsOnlyEmptySecretPlaceholders()
    {
        var secretPaths = new[]
        {
            new[] { "Jwt", "Key" },
            new[] { "Auth", "OtpPepper" },
            new[] { "Storage", "AccessKey" },
            new[] { "Storage", "SecretKey" },
            new[] { "AWS", "S3", "AccessKey" },
            new[] { "AWS", "S3", "SecretKey" },
            new[] { "Kavenegar", "ApiKey" },
        };

        var populated = secretPaths
            .Where(path => !string.IsNullOrWhiteSpace(SecurityInvariantHelpers.GetString(Settings, path)))
            .Select(path => string.Join(':', path))
            .ToList();

        populated.ShouldBeEmpty(
            "secrets must be supplied through .env / the secret store, never committed in appsettings.json: " +
            string.Join(", ", populated));
    }

    [Fact]
    public void PaymentWebhookFlags_ArePresentAndSignatureIsRequired()
    {
        var signatureRequired = SecurityInvariantHelpers.GetBoolean(
            Settings, "FeatureManagement", "Payment.Callback.SignatureRequired");
        signatureRequired.ShouldBeTrue("the callback nonce must always be validated (SEC-06)");

        // The flag exists for every deployment so production can turn it on
        // (ValidateProductionHardening enforces it in Production).
        var section = SecurityInvariantHelpers.GetSection(Settings, "FeatureManagement");
        SecurityInvariantHelpers
            .TryGetProperty(section, "Payment.Callback.IpWhitelistRequired", out _)
            .ShouldBeTrue("FeatureManagement:Payment.Callback.IpWhitelistRequired must be declared in appsettings.json");
    }

    [Fact]
    public void AdminIpWhitelist_IsConfigured()
    {
        var whitelist = SecurityInvariantHelpers.GetStringArray(Settings, "Security", "AdminIpWhitelist");

        whitelist.ShouldNotBeEmpty("admin routes are fail-closed (SEC-05); the whitelist must not be empty");
        whitelist.ShouldAllBe(entry => !string.IsNullOrWhiteSpace(entry));
    }

    [Fact]
    public void AllowedOrigins_AreExplicitWithoutWildcards()
    {
        var origins = SecurityInvariantHelpers.GetStringArray(Settings, "Security", "AllowedOrigins");

        origins.ShouldNotBeEmpty("the redirect/CORS allow-list must stay explicit (SEC-11/SEC-12)");
        origins.ShouldNotContain("*");
        origins.ShouldAllBe(origin => origin.StartsWith("http", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SecurityHeaders_StayEnabled()
    {
        SecurityInvariantHelpers.GetBoolean(Settings, "SecurityHeaders", "DisableCsp").ShouldBeFalse();

        foreach (var flag in new[] { "EnableNonce", "EnableCoop", "EnableCoep", "EnableCorp" })
        {
            SecurityInvariantHelpers.GetBoolean(Settings, "SecurityHeaders", flag)
                .ShouldBeTrue($"SecurityHeaders:{flag} must stay enabled (phase 6, item 4)");
        }

        SecurityInvariantHelpers.GetInt32(Settings, "SecurityHeaders", "HstsMaxAge")
            .ShouldBeGreaterThan(0);
    }

    [Fact]
    public void MockPaymentGateway_IsDisabledInCommittedConfiguration()
    {
        SecurityInvariantHelpers.GetBoolean(Settings, "PaymentGateway", "EnableMockGateway")
            .ShouldBeFalse("a mock gateway outside Development would let anyone forge payments (SEC-23)");
    }
}
