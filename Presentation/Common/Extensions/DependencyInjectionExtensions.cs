using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;

namespace Presentation.Common.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationPipelines(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IHttpResultMapper, HttpResultMapper>();
        return services;
    }

    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}