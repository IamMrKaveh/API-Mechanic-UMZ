namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        services.AddScoped<IAdminCategoryService, AdminCategoryService>();
        services.AddScoped<IAdminCategoryGroupService, AdminCategoryGroupService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IShippingMethodService, ShippingMethodService>();
        services.AddScoped<IAuditService, Application.Services.AuditService>();

        services.AddScoped<IHtmlSanitizer, HtmlSanitizer>();

        return services;
    }
}