namespace Application.Brand.Mapping;

public sealed class BrandMappingProfile : Profile
{
    public BrandMappingProfile()
    {
        CreateMap<Domain.Brand.Aggregates.Brand, BrandSummaryDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.ActiveProductCount, opt => opt.MapFrom(src => src.ActiveProductsCount))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Brand.Aggregates.Brand, BrandTreeDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.ActiveProductsCount));

        CreateMap<Domain.Brand.Aggregates.Brand, BrandListItemDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name.Value : string.Empty))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Brand.Aggregates.Brand, BrandDetailDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name.Value : string.Empty))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.ActiveProductCount, opt => opt.MapFrom(src => src.ActiveProductsCount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Brand.Aggregates.Brand, BrandHierarchyDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name));

        CreateMap<Domain.Brand.Aggregates.Brand, BrandViewDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name.Value : string.Empty))
            .ForMember(dest => dest.ActiveProductsCount, opt => opt.MapFrom(src => src.ActiveProductsCount))
            .ForMember(dest => dest.TotalProductsCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());
    }
}