namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        services.AddScoped<IAdminCategoryService, AdminCategoryService>();
        services.AddScoped<IAdminCategoryGroupService, AdminCategoryGroupService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminOrderStatusService, AdminOrderStatusService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IAdminReviewService, AdminReviewService>();
        services.AddScoped<IAdminShippingMethodService, AdminShippingMethodService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminInventoryService, AdminInventoryService>();
        services.AddScoped<IAdminDiscountService, AdminDiscountService>();
        services.AddScoped<IAdminAttributeService, AdminAttributeService>();
        services.AddScoped<IAdminProductVariantShippingService, AdminProductVariantShippingService>();

        services.AddScoped<IShippingMethodService, ShippingMethodService>();
        services.AddScoped<ICheckoutShippingService, CheckoutShippingService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICategoryGroupService, CategoryGroupService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderItemService, OrderItemService>();
        services.AddScoped<IOrderStatusService, OrderStatusService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var url = config["Elasticsearch:Url"] ?? "http://localhost:4200";
            var settings = new ElasticsearchClientSettings(new Uri(url))
                .DefaultIndex("products_v1");
            return new ElasticsearchClient(settings);
        });

        services.AddSingleton<IHtmlSanitizer>(new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" },
            AllowedAttributes = { "class" },
            AllowedSchemes = { "http", "https" }
        }));

        return services;
    }
}