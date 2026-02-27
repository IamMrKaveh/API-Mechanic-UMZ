namespace Application.Order.Mapping;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<AddressSnapshot, UserAddressDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.OriginalAddressId))
            .ForMember(dest => dest.IsDefault, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore());

        CreateMap<Domain.Order.Order, OrderDto>()
            .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.OrderNumber.Value))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.Value))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.DisplayName))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount.Amount))
            .ForMember(dest => dest.TotalProfit, opt => opt.MapFrom(src => src.TotalProfit.Amount))
            .ForMember(dest => dest.ShippingCost, opt => opt.MapFrom(src => src.ShippingCost.Amount))
            .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.DiscountAmount.Amount))
            .ForMember(dest => dest.FinalAmount, opt => opt.MapFrom(src => src.FinalAmount.Amount))
            .ForMember(dest => dest.UserAddress, opt => opt.MapFrom(src => src.AddressSnapshot))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.OrderStatusId, opt => opt.Ignore())
            .ForMember(dest => dest.Shipping, opt => opt.Ignore());

        CreateMap<Domain.Order.Order, AdminOrderDto>()
            .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.OrderNumber.Value))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.Value))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.DisplayName))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount.Amount))
            .ForMember(dest => dest.TotalProfit, opt => opt.MapFrom(src => src.TotalProfit.Amount))
            .ForMember(dest => dest.ShippingCost, opt => opt.MapFrom(src => src.ShippingCost.Amount))
            .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.DiscountAmount.Amount))
            .ForMember(dest => dest.FinalAmount, opt => opt.MapFrom(src => src.FinalAmount.Amount))
            .ForMember(dest => dest.UserAddress, opt => opt.MapFrom(src => src.AddressSnapshot))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.OrderStatusId, opt => opt.Ignore())
            .ForMember(dest => dest.Shipping, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.OrderItemsCount, opt => opt.MapFrom(src => src.OrderItems.Count));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Product != null
                        ? src.Variant.Product.Name.Value
                        : src.ProductName))
            .ForMember(dest => dest.VariantSku,
                opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Sku != null
                        ? src.Variant.Sku.Value
                        : null))
            .ForMember(dest => dest.PurchasePriceAtOrder, opt => opt.MapFrom(src => src.PurchasePriceAtOrder.Amount))
            .ForMember(dest => dest.SellingPriceAtOrder, opt => opt.MapFrom(src => src.SellingPriceAtOrder.Amount))
            .ForMember(dest => dest.OriginalPriceAtOrder, opt => opt.MapFrom(src => src.OriginalPriceAtOrder.Amount))
            .ForMember(dest => dest.DiscountAtOrder, opt => opt.MapFrom(src => src.DiscountAtOrder.Amount))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.Profit, opt => opt.MapFrom(src => src.Profit.Amount))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.SellingPriceAtOrder.Amount))
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.ProductIcon, opt => opt.Ignore())
            .ForMember(dest => dest.Attributes, opt => opt.Ignore());

        CreateMap<AddressSnapshot, AddressSnapshotDto>();

        CreateMap<OrderStatus, OrderStatusDto>();

        CreateMap<OrderStatistics, OrderStatisticsDto>();
    }
}