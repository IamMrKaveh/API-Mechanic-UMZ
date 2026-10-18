using System.IdentityModel.Tokens.Jwt;
using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Infrastructure.Security.Settings;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Presentation.Common.Extensions;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddApplicationAuthentication(this WebApplicationBuilder builder)
    {
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
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var settings = jwtOptions.Value;

                bearerOptions.MapInboundClaims = true;
                bearerOptions.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                };

                bearerOptions.Events ??= new JwtBearerEvents();

                var previousOnTokenValidated = bearerOptions.Events.OnTokenValidated;
                bearerOptions.Events.OnTokenValidated = async context =>
                {
                    if (previousOnTokenValidated is not null)
                        await previousOnTokenValidated(context);

                    var alg = context.SecurityToken is JsonWebToken jwt
                        ? jwt.Alg
                        : (context.SecurityToken as JwtSecurityToken)?.Header?.Alg;

                    if (!string.Equals(alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                        context.Fail("Invalid token signing algorithm.");

                    // Revoked sessions must lose access before their token expires
                    // (logout-everywhere, password change, admin revocation).
                    var sessionIdClaim = context.Principal!.FindFirst("sid")?.Value;
                    if (!Guid.TryParse(sessionIdClaim, out var sessionId))
                    {
                        context.Fail("Token is not bound to a session.");
                        return;
                    }

                    var revocationChecker =
                        context.HttpContext.RequestServices.GetService<ISessionRevocationChecker>();
                    if (revocationChecker is not null &&
                        !await revocationChecker.IsSessionActiveAsync(
                            sessionId, context.HttpContext.RequestAborted))
                    {
                        context.Fail("Session revoked.");
                    }
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
