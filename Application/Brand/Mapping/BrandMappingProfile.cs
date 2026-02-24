namespace Application.Brand.Mapping;

public sealed class BrandMappingProfile : Profile
{
    public BrandMappingProfile()
    {
        CreateMap<Domain.Brand.Brand, BrandSummaryDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.ActiveProductCount, opt => opt.MapFrom(src => src.ActiveProductsCount));

        CreateMap<Domain.Brand.Brand, BrandTreeDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.ActiveProductsCount));

        CreateMap<Domain.Brand.Brand, BrandListItemDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name.Value : string.Empty))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));

        CreateMap<Domain.Brand.Brand, BrandDetailDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name.Value : string.Empty))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.ActiveProductCount, opt => opt.MapFrom(src => src.ActiveProductsCount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));

        CreateMap<Domain.Brand.Brand, BrandHierarchyDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name));

        CreateMap<Domain.Brand.Brand, BrandViewDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name.Value : string.Empty))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));
    }
}