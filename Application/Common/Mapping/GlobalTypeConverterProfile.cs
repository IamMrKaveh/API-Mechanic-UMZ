namespace Application.Common.Mapping;

public sealed class GlobalTypeConverterProfile : Profile
{
    public GlobalTypeConverterProfile()
    {
        
        CreateMap<BrandName, string>().ConvertUsing(src => src.Value);
        CreateMap<CategoryName, string>().ConvertUsing(src => src.Value);
        CreateMap<ProductName, string>().ConvertUsing(src => src.Value);
        CreateMap<Slug, string?>().ConvertUsing(src => src != null ? src.Value : null);
        CreateMap<Money, decimal>().ConvertUsing(src => src.Amount);
        CreateMap<Sku, string>().ConvertUsing(src => src.Value);

        
        CreateMap<BrandName?, string?>().ConvertUsing(src => src != null ? src.Value : null);
        CreateMap<Money?, decimal?>().ConvertUsing(src => src != null ? src.Amount : null);
        CreateMap<Sku?, string?>().ConvertUsing(src => src != null ? src.Value : null);
    }
}