namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        // Domain Services
        services.AddScoped<PriceCalculatorService>();
        services.AddScoped<InventoryDomainService>();

        // Legacy Services (To be removed after full CQRS migration)
        services.AddScoped<IAdminCategoryService, AdminCategoryService>();
        services.AddScoped<IAdminCategoryGroupService, AdminCategoryGroupService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminOrderStatusService, AdminOrderStatusService>();
        // IAdminProductService removed as it is replaced by CQRS
        services.AddScoped<IAdminReviewService, AdminReviewService>();
        services.AddScoped<IAdminShippingMethodService, AdminShippingMethodService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminInventoryService, AdminInventoryService>();
        services.AddScoped<IAdminDiscountService, AdminDiscountService>();
        services.AddScoped<IAdminAttributeService, AdminAttributeService>();
        services.AddScoped<IAdminProductVariantShippingService, AdminProductVariantShippingService>();
        services.AddScoped<IAdminMediaService, AdminMediaService>();

        services.AddScoped<IShippingMethodService, ShippingMethodService>();
        services.AddScoped<ICheckoutShippingService, CheckoutShippingService>();

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
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}