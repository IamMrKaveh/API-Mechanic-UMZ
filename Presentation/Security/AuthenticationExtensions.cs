using Infrastructure.Security.Settings;

namespace Presentation.Security;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddApplicationAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection(JwtSettings.SectionName));

        builder.Services.Configure<GoogleAuthSettings>(
            builder.Configuration.GetSection(GoogleAuthSettings.SectionName));

        var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];

        var googleClientId =
            builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;

        var googleClientSecret =
            builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;

        var authBuilder = builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        if (!string.IsNullOrWhiteSpace(googleClientId) &&
            !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SaveTokens = true;
            });
        }

        return builder;
    }
}