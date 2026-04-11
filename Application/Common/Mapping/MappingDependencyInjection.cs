using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Common.Mapping;

public static class MappingDependencyInjection
{
    public static IServiceCollection AddApplicationMappings(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();

        config.Scan(Assembly.GetExecutingAssembly());
        config.Compile();

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}