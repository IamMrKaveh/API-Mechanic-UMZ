namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LedkaContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));



        return services;
    }
}