namespace Application.Inventory.Mapping;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Product != null
                        ? src.Variant.Product.Name.Value
                        : string.Empty))
            .ForMember(dest => dest.VariantSku,
                opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Sku != null
                        ? src.Variant.Sku.Value
                        : string.Empty))
            .ForMember(dest => dest.Sku,
                opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Sku != null
                        ? src.Variant.Sku.Value
                        : string.Empty))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src =>
                    src.User != null
                        ? $"{src.User.FirstName} {src.User.LastName}".Trim()
                        : string.Empty));
    }
}