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

        services
            .AddControllers(options =>
            {
                options.Filters.AddService<OtpRateLimitFilter>();
                options.Filters.AddService<ReviewRateLimitFilter>();
                options.Filters.Add<ValidationFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
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
            options.EnableAnnotations();

            options.CustomOperationIds(api =>
            {
                if (api.ActionDescriptor is ControllerActionDescriptor descriptor)
                {
                    var attr = descriptor.MethodInfo
                        .GetCustomAttributes(typeof(SwaggerOperationAttribute), false)
                        .Cast<SwaggerOperationAttribute>()
                        .FirstOrDefault();

                    if (attr is not null && !string.IsNullOrWhiteSpace(attr.OperationId))
                        return attr.OperationId;

                    var controller = descriptor.ControllerName.Replace("Controller", string.Empty, StringComparison.OrdinalIgnoreCase);
                    return $"{controller}_{descriptor.ActionName}";
                }

                return api.RelativePath?.Replace("/", "_") ?? Guid.NewGuid().ToString("N");
            });

            options.OperationFilter<RemoveVersionParameterOperationFilter>();
            options.OperationFilter<DefaultResponseOperationFilter>();
            options.OperationFilter<CustomOperationIdOperationFilter>();
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
