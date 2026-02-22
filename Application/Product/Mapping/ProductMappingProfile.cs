namespace Application.Product.Mapping;

public sealed class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Domain.Product.Product, AdminProductListItemDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Brand != null && src.Brand.Category != null ? src.Brand.Category.Name.Value : "N/A"))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name.Value : "N/A"))
            .ForMember(dest => dest.TotalStock, opt => opt.MapFrom(src => src.Stats.TotalStock))
            .ForMember(dest => dest.MinPrice, opt => opt.MapFrom(src => src.Stats.MinPrice))
            .ForMember(dest => dest.MaxPrice, opt => opt.MapFrom(src => src.Stats.MaxPrice))
            .ForMember(dest => dest.Sku, opt => opt.Ignore())
            .ForMember(dest => dest.VariantCount, opt => opt.Ignore())
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Product.Product, AdminProductDetailDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64()))
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.Sku, opt => opt.Ignore())
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<ProductVariant, ProductVariantViewDto>()
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku))
            .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => src.PurchasePrice))
            .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src => src.SellingPrice))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.OriginalPrice))
            .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64()))
            .ForMember(dest => dest.EnabledShippingIds, opt => opt.MapFrom(src => src.ProductVariantShippings.Where(sm => sm.IsActive).Select(sm => sm.ShippingId).ToList()))
            .ForMember(dest => dest.Attributes, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<Domain.Product.Product, ProductDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64()))
            .ForMember(dest => dest.Sku, opt => opt.Ignore());
    }
}