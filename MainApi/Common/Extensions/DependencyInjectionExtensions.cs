using Asp.Versioning;

namespace MainApi.Common.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationPipelines(this IServiceCollection services)
    {
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
        }).AddMvc();

        return services;
    }
}