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

        return builder;
    }
}
