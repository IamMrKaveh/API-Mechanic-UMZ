using Application.Auth.Contracts;
using Infrastructure.Cache.Options;
using SharedContracts.FeatureManagement;

namespace Presentation.Common.Extensions;

public static class ConfigurationValidationExtension
{
    public static WebApplicationBuilder ValidateRequiredConfiguration(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        ValidateConnectionStrings(configuration);
        ValidateJwtSettings(configuration);
        ValidateExternalServices(configuration);
        configuration.ValidateFeatureFlagsPresence();
        ValidateProductionCacheAndRateLimit(configuration, builder.Environment);
        ValidateCacheEncryption(configuration, builder.Environment);
        ValidateInitialAdmin(configuration, builder.Environment);
        ValidateProductionHardening(configuration, builder.Environment);

        return builder;
    }

    private static void ValidateConnectionStrings(IConfiguration configuration)
    {
        var missing = new List<string>();

        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnection))
            missing.Add("ConnectionStrings:DefaultConnection");

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing required connection string configuration: {string.Join(", ", missing)}.");
    }

    private static void ValidateJwtSettings(ConfigurationManager configuration)
    {
        var section = configuration.GetSection("Jwt");

        if (!section.Exists())
            throw new InvalidOperationException("Configuration section 'Jwt' is missing.");

        var missing = new List<string>();

        var key = section["Key"];
        if (string.IsNullOrWhiteSpace(key))
            missing.Add("Jwt:Key");
        else if (System.Text.Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException(
                "Jwt:Key must be at least 256 bits (32 bytes) long.");

        if (string.IsNullOrWhiteSpace(section["Issuer"]))
            missing.Add("Jwt:Issuer");

        if (string.IsNullOrWhiteSpace(section["Audience"]))
            missing.Add("Jwt:Audience");

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing required JWT configuration: {string.Join(", ", missing)}.");
    }

    private static void ValidateExternalServices(ConfigurationManager configuration)
    {
        var missing = new List<string>();

        var storageSection = configuration.GetSection("Storage");
        if (!storageSection.Exists())
            missing.Add("Storage");
        else
        {
            if (string.IsNullOrWhiteSpace(storageSection["Provider"]))
                missing.Add("Storage:Provider");
            if (string.IsNullOrWhiteSpace(storageSection["BucketName"]))
                missing.Add("Storage:BucketName");
            if (string.IsNullOrWhiteSpace(storageSection["AccessKey"]))
                missing.Add("Storage:AccessKey");
            if (string.IsNullOrWhiteSpace(storageSection["SecretKey"]))
                missing.Add("Storage:SecretKey");
        }

        var kavenegarSection = configuration.GetSection("Kavenegar");
        if (kavenegarSection.Exists() && string.IsNullOrWhiteSpace(kavenegarSection["ApiKey"]))
            missing.Add("Kavenegar:ApiKey");

        var zarinpalSection = configuration.GetSection("ZarinPal");
        if (zarinpalSection.Exists() && string.IsNullOrWhiteSpace(zarinpalSection["MerchantId"]))
            missing.Add("ZarinPal:MerchantId");

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing required external-service configuration: {string.Join(", ", missing)}.");
    }

    private static void ValidateInitialAdmin(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(IInitialAdminOptions.SectionName);
        if (!section.Exists())
            return;

        // SEC-25: this list auto-promotes matching phone numbers to Admin on
        // first login. It is a bootstrap mechanism only: in Production it must
        // be supplied via environment (InitialAdmin__PhoneNumbers__0=...) and
        // emptied once bootstrap admins exist.
        var phoneNumbers = section.GetSection("PhoneNumbers").GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (phoneNumbers.Count == 0)
            return;

        if (environment.IsProduction())
        {
            Log.Warning(
                "InitialAdmin:PhoneNumbers contains {Count} entry(ies) in Production; anyone logging in with these numbers is promoted to Admin. Empty the list after bootstrap is complete.",
                phoneNumbers.Count);
        }
    }

    /// <summary>
    /// Production-only security posture gate (phase 5 of the security audit). Every check
    /// mirrors an audit finding whose fix only works if the deployment actually configures
    /// it; failing fast at startup is preferable to silently running with a weakened posture.
    /// All problems are reported at once so the operator can fix the list in one pass.
    /// </summary>
    private static void ValidateProductionHardening(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
            return;

        var problems = new List<string>();

        // SEC-01: the previously committed signing key must never be reused.
        var jwtKey = configuration["Jwt:Key"];
        if (!string.IsNullOrWhiteSpace(jwtKey)
            && System.Text.RegularExpressions.Regex.IsMatch(
                jwtKey, "Breaking[0-9]+Bad", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            problems.Add(
                "Jwt:Key still matches the signing key that was exposed in Git history (SEC-01). " +
                "Rotate it and set Jwt__Key from the secret store.");
        }

        // SEC-08: OTP hashes are only resistant to offline cracking with a server pepper.
        var otpPepper = configuration["Auth:OtpPepper"];
        if (string.IsNullOrWhiteSpace(otpPepper) || otpPepper.Length < 16)
        {
            problems.Add(
                "Auth:OtpPepper must be configured with at least 16 characters in Production (SEC-08). " +
                "Set Auth__OtpPepper from the secret store.");
        }

        // SEC-20: AllowedHosts="*" allows Host header injection.
        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts)
            || allowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(host => host == "*"))
        {
            problems.Add(
                "AllowedHosts must list the real hostnames in Production; the wildcard '*' is rejected (SEC-20).");
        }

        // SEC-05: admin routes are fail-closed, but an empty whitelist would lock out all
        // administrators; require the operator to state the real IPs instead.
        if (GetSectionValues(configuration, "Security:AdminIpWhitelist").Count == 0)
        {
            problems.Add(
                "Security:AdminIpWhitelist must list the operations-room IPs in Production (SEC-05).");
        }

        // SEC-11/SEC-12: the redirect allow-list is also the CORS origin list.
        var allowedOrigins = GetSectionValues(configuration, "Security:AllowedOrigins");
        if (allowedOrigins.Count == 0)
        {
            problems.Add("Security:AllowedOrigins must list the real frontend origins in Production (SEC-11).");
        }
        else if (allowedOrigins.Any(origin => origin.Contains('*')))
        {
            problems.Add("Security:AllowedOrigins must not contain wildcards in Production (SEC-12).");
        }

        // SEC-06: the webhook IP allow-list is only enforced when the flag is on; an empty
        // list with the flag on would reject every gateway callback, so require both.
        var ipWhitelistFlag =
            $"{FeatureManagementExtensions.SectionName}:{FeatureFlags.PaymentCallbackIpWhitelistRequired}";
        if (!IsEnabled(configuration, ipWhitelistFlag))
        {
            problems.Add($"{ipWhitelistFlag} must be true in Production (SEC-06).");
        }

        if (GetSectionValues(configuration, "Zarinpal:AllowedIps").Count == 0)
        {
            problems.Add(
                "Zarinpal:AllowedIps must list the payment gateway's official callback IPs in Production (SEC-06).");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Production security configuration is incomplete:" + Environment.NewLine +
                string.Join(Environment.NewLine, problems.Select(problem => $" - {problem}")));
        }

        if (IsEnabled(configuration, "Swagger:Enabled"))
        {
            Log.Warning(
                "Swagger:Enabled is true in Production; the OpenAPI surface is publicly reachable. " +
                "Set Swagger:Enabled=false unless the API is protected by network policy.");
        }
    }

    private static List<string> GetSectionValues(IConfiguration configuration, string sectionPath)
        => configuration.GetSection(sectionPath).GetChildren()
            .Select(child => child.Value)
            .OfType<string>()
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

    private static bool IsEnabled(IConfiguration configuration, string key)
        => bool.TryParse(configuration[key], out var enabled) && enabled;

    private static void ValidateProductionCacheAndRateLimit(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
            return;

        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        if (!cacheOptions.UseRedis)
            throw new InvalidOperationException(
                "Cache:UseRedis must be true in Production so that rate-limiting and idempotency are distributed. " +
                "Set Cache:UseRedis=true and provide Cache:RedisConnectionString or ConnectionStrings:Redis.");

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Cache:RedisConnectionString"];

        if (string.IsNullOrWhiteSpace(redisConnection))
            throw new InvalidOperationException(
                "Cache:RedisConnectionString (or ConnectionStrings:Redis) is required in Production when Cache:UseRedis=true.");

        if (!redisConnection.Contains("ssl=true", StringComparison.OrdinalIgnoreCase)
            && !redisConnection.Contains("ssl=True", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Redis connection string must enable TLS in Production (append 'ssl=true' to the connection string).");
        }
    }

    private static void ValidateCacheEncryption(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(Infrastructure.Cache.Options.CacheEncryptionOptions.SectionName);
        if (!section.Exists())
            return;

        var options = section.Get<Infrastructure.Cache.Options.CacheEncryptionOptions>();
        if (options is null || !options.IsEnabled)
            return;

        if (string.IsNullOrWhiteSpace(options.KeyBase64))
            throw new InvalidOperationException(
                "Cache:Encryption:KeyBase64 is required when Cache:Encryption:IsEnabled=true.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(options.KeyBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Cache:Encryption:KeyBase64 is not a valid Base64 string.");
        }

        if (key.Length != 32)
            throw new InvalidOperationException(
                "Cache:Encryption:KeyBase64 must decode to a 256-bit (32-byte) key.");

        if (environment.IsProduction() && !options.UseTls)
            throw new InvalidOperationException(
                "Cache:Encryption:UseTls must be true in Production.");
    }
}
