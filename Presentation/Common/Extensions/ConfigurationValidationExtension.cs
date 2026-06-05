namespace Presentation.Common.Extensions;

public static class ConfigurationValidationExtension
{
    public static WebApplicationBuilder ValidateRequiredConfiguration(this WebApplicationBuilder builder)
    {
        var requiredKeys = new[]
        {
            "ConnectionStrings:PoolerConnection",
            "ConnectionStrings:DirectConnection",
            "ConnectionStrings:Redis",
            "Jwt:Key",
            "Jwt:Issuer",
            "Jwt:Audience",
            "AWS:S3:BucketName",
            "AWS:S3:Region",
            "AWS:S3:AccessKey",
            "AWS:S3:SecretKey",
            "AWS:S3:Endpoint",
            "AWS:S3:BaseUrl",
            "Zarinpal:MerchantId",
            "FrontendUrls:BaseUrl"
        };

        var missing = requiredKeys
            .Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key]))
            .ToList();

        if (missing.Count != 0)
        {
            throw new InvalidOperationException(
                $"Required configuration keys are missing or empty: {string.Join(", ", missing)}. " +
                "Ensure all required environment variables are set.");
        }

        var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException("Security:AllowedOrigins باید حداقل یک origin داشته باشد.");

        return builder;
    }
}