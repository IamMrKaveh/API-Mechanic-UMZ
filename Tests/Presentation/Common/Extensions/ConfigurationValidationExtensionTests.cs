using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Common.Extensions;
using SharedContracts.FeatureManagement;

namespace Tests.Presentation.Common.Extensions;

public class ConfigurationValidationExtensionTests
{
    private static Dictionary<string, string?> ValidConfig() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;",
        ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
        ["Jwt:Issuer"] = "issuer",
        ["Jwt:Audience"] = "audience",
        ["Storage:Provider"] = "S3",
        ["Storage:BucketName"] = "bucket",
        ["Storage:AccessKey"] = "key",
        ["Storage:SecretKey"] = "secret",
        [$"FeatureManagement:{FeatureFlags.PaymentCallbackSignatureRequired}"] = "true",
        [$"FeatureManagement:{FeatureFlags.IdempotencyDistributedLockEnabled}"] = "false",
        [$"FeatureManagement:{FeatureFlags.SagaAutoRefundOnCommitFailure}"] = "false",
        [$"FeatureManagement:{FeatureFlags.StoragePresignedUrlEnabled}"] = "false",
        [$"FeatureManagement:{FeatureFlags.AdminWalletLedgerV2Enabled}"] = "false"
    };

    private static WebApplicationBuilder BuildBuilder(
        Dictionary<string, string?> values,
        string environment = "Development")
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = environment;
        // Isolate from ambient appsettings.json / environment variables so each
        // scenario only sees the configuration under test.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithValidConfig_Passes()
    {
        var builder = BuildBuilder(ValidConfig());

        Should.NotThrow(() => builder.ValidateRequiredConfiguration());
    }

    [Fact]
    public void ValidateRequiredConfiguration_ReturnsSameBuilder()
    {
        var builder = BuildBuilder(ValidConfig());

        builder.ValidateRequiredConfiguration().ShouldBeSameAs(builder);
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithoutConnectionString_Throws()
    {
        var values = ValidConfig();
        values.Remove("ConnectionStrings:DefaultConnection");
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("ConnectionStrings:DefaultConnection");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithoutJwtSection_Throws()
    {
        var values = ValidConfig();
        values.Remove("Jwt:Key");
        values.Remove("Jwt:Issuer");
        values.Remove("Jwt:Audience");
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("'Jwt'");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithShortJwtKey_Throws()
    {
        var values = ValidConfig();
        values["Jwt:Key"] = "short";
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("256 bits");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithoutJwtIssuer_Throws()
    {
        var values = ValidConfig();
        values.Remove("Jwt:Issuer");
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Jwt:Issuer");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithoutStorageSection_Throws()
    {
        var values = ValidConfig();
        foreach (var key in values.Keys.Where(k => k.StartsWith("Storage:")).ToList())
            values.Remove(key);
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Storage");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithKavenegarButNoApiKey_Throws()
    {
        var values = ValidConfig();
        values["Kavenegar:ApiKey"] = "";
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Kavenegar:ApiKey");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithZarinPalButNoMerchantId_Throws()
    {
        var values = ValidConfig();
        values["ZarinPal:MerchantId"] = "  ";
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("ZarinPal:MerchantId");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithoutFeatureManagement_Throws()
    {
        var values = ValidConfig();
        foreach (var key in values.Keys.Where(k => k.StartsWith("FeatureManagement:")).ToList())
            values.Remove(key);
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("FeatureManagement");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ProductionWithoutRedis_Throws()
    {
        var builder = BuildBuilder(ValidConfig(), Environments.Production);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Cache:UseRedis");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ProductionWithRedisButNoConnection_Throws()
    {
        var values = ValidConfig();
        values["Cache:UseRedis"] = "true";
        var builder = BuildBuilder(values, Environments.Production);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Cache:RedisConnectionString");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ProductionWithRedisButNoTls_Throws()
    {
        var values = ValidConfig();
        values["Cache:UseRedis"] = "true";
        values["Cache:RedisConnectionString"] = "localhost:6379";
        var builder = BuildBuilder(values, Environments.Production);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("ssl=true");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ProductionWithTlsRedis_Passes()
    {
        var values = ValidConfig();
        values["Cache:UseRedis"] = "true";
        values["Cache:RedisConnectionString"] = "localhost:6379,ssl=true";
        var builder = BuildBuilder(values, Environments.Production);

        Should.NotThrow(() => builder.ValidateRequiredConfiguration());
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithEncryptionEnabledButNoKey_Throws()
    {
        var values = ValidConfig();
        values["Cache:Encryption:IsEnabled"] = "true";
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Cache:Encryption:KeyBase64");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithInvalidEncryptionKey_Throws()
    {
        var values = ValidConfig();
        values["Cache:Encryption:IsEnabled"] = "true";
        values["Cache:Encryption:KeyBase64"] = "not-base64!!!";
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Base64");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithShortEncryptionKey_Throws()
    {
        var values = ValidConfig();
        values["Cache:Encryption:IsEnabled"] = "true";
        values["Cache:Encryption:KeyBase64"] = Convert.ToBase64String(new byte[16]);
        var builder = BuildBuilder(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("32-byte");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ProductionWithEncryptionButNoTls_Throws()
    {
        var values = ValidConfig();
        values["Cache:UseRedis"] = "true";
        values["Cache:RedisConnectionString"] = "localhost:6379,ssl=true";
        values["Cache:Encryption:IsEnabled"] = "true";
        values["Cache:Encryption:KeyBase64"] = Convert.ToBase64String(new byte[32]);
        values["Cache:Encryption:UseTls"] = "false";
        var builder = BuildBuilder(values, Environments.Production);

        var ex = Should.Throw<InvalidOperationException>(() =>
            builder.ValidateRequiredConfiguration());

        ex.Message.ShouldContain("Cache:Encryption:UseTls");
    }

    [Fact]
    public void ValidateRequiredConfiguration_WithValidEncryptionKey_Passes()
    {
        var values = ValidConfig();
        values["Cache:Encryption:IsEnabled"] = "true";
        values["Cache:Encryption:KeyBase64"] = Convert.ToBase64String(new byte[32]);
        var builder = BuildBuilder(values);

        Should.NotThrow(() => builder.ValidateRequiredConfiguration());
    }
}
