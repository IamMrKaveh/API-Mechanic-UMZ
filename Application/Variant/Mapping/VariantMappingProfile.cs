namespace Application.Variant.Mapping;

public class VariantMappingProfile : Profile
{
    public VariantMappingProfile()
    {
        CreateMap<ProductVariant, ProductVariantDto>()
            .ConstructUsing(src => new ProductVariantDto(
                src.Id,
                src.ProductId,
                src.Sku.Value,
                src.SellingPrice.Amount,
                src.SellingPrice.Amount,
                src.StockQuantity))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.SellingPrice.Amount))
            .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.SellingPrice.Amount));
    }
}