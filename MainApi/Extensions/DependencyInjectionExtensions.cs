namespace MainApi.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationPipelines(this IServiceCollection services)
    {
        // ثبت تمام Validator های موجود در لایه Application
        //services.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(Application.ServiceCollectionExtensions)));

        // ثبت Pipeline Behavior ها به ترتیب اجرا
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // اگر TransactionBehavior را پیاده‌سازی کرده‌اید:
        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}