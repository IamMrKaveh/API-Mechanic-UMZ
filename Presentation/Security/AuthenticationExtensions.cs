using Infrastructure.Security.Settings;

namespace Presentation.Security;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddApplicationAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.Configure<GoogleAuthSettings>(
            builder.Configuration.GetSection(GoogleAuthSettings.SectionName));

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        builder.Services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((bearerOptions, jwtSettings) =>
            {
                var settings = jwtSettings.Value;
                bearerOptions.RequireHttpsMetadata = true;
                bearerOptions.SaveToken = true;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        var googleClientId =
            builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        var googleClientSecret =
            builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(googleClientId) &&
            !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            builder.Services
                .AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.SaveTokens = true;
                });
        }

        return builder;
    }
}