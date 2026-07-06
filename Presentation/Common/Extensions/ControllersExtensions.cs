using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Common.Swagger;

namespace Presentation.Common.Extensions;

public static class ControllersExtensions
{
    public static IServiceCollection AddPresentationControllers(
        this IServiceCollection services)
    {
        services.AddCustomApiVersioning();

        services.AddControllers(options =>
        {
            options.Filters.AddService<OtpRateLimitFilter>();
            options.Filters.Add<ValidationFilter>();
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddScoped<IHttpResultMapper, HttpResultMapper>();

        services.AddSwaggerServices();

        return services;
    }

    private static IServiceCollection AddSwaggerServices(
        this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigureOptions>();

        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<RemoveVersionParameterOperationFilter>();
            options.OperationFilter<DefaultResponseOperationFilter>();
            options.SchemaFilter<NullableSchemaFilter>();

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}