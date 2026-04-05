namespace MainApi.Common.Extensions;

public static class ConfigurationValidationExtension
{
    public static WebApplicationBuilder ValidateRequiredConfiguration(this WebApplicationBuilder builder)
    {
        var requiredKeys = new[]
        {
            "ConnectionStrings:PoolerConnection",
            "ConnectionStrings:DirectConnection",
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

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Required configuration keys are missing or empty: {string.Join(", ", missing)}. " +
                "Ensure all required environment variables are set.");
        }

        return builder;
    }
}