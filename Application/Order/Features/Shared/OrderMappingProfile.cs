namespace Application.Features.Orders.Shared;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // Order -> OrderDto
        CreateMap<Domain.Order.Order, OrderDto>()
            .ForMember(dest => dest.UserAddress, opt => opt.MapFrom(src =>
                AddressSnapshot.FromJson(src.AddressSnapshot.ToJson())))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src =>
                src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

        // Order -> AdminOrderDto
        CreateMap<Domain.Order.Order, AdminOrderDto>()
            .ForMember(dest => dest.UserAddress, opt => opt.MapFrom(src =>
                AddressSnapshot.FromJson(src.AddressSnapshot.ToJson())))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src =>
                src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.OrderItemsCount, opt => opt.MapFrom(src => src.OrderItems.Count));

        // OrderItem -> OrderItemDto
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src =>
                src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : null))
            .ForMember(dest => dest.ProductIcon, opt => opt.Ignore()) // Set manually
            .ForMember(dest => dest.Attributes, opt => opt.Ignore()); // Set manually

        // AddressSnapshot -> AddressSnapshotDto
        CreateMap<AddressSnapshot, AddressSnapshotDto>();

        // OrderStatus -> OrderStatusDto
        CreateMap<OrderStatus, OrderStatusDto>();

        // ShippingMethod -> ShippingMethodDto
        CreateMap<ShippingMethod, ShippingMethodDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src =>
                src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

        // User -> UserSummaryDto
        CreateMap<Domain.User.User, UserSummaryDto>();

        // OrderStatistics -> OrderStatisticsDto
        CreateMap<OrderStatistics, OrderStatisticsDto>();
    }
}