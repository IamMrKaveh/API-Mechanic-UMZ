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
            "LiaraStorage:AccessKey",
            "LiaraStorage:SecretKey",
            "LiaraStorage:BucketName",
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