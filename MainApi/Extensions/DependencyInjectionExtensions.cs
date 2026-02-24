namespace MainApi.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationPipelines(this IServiceCollection services)
    {
        
        

        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        

        return services;
    }
}