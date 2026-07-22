using Infrastructure.Cache.Options;

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
