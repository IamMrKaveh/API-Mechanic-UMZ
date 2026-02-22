namespace Application.Variant.Mapping;

public class VariantMappingProfile : Profile
{
    public VariantMappingProfile()
    {
        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.SellingPrice))
            .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.SellingPrice));
    }
}