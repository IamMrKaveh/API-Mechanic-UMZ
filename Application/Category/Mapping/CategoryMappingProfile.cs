namespace Application.Category.Mapping;

public sealed class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Domain.Category.Aggregates.Category, CategoryDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.ActiveGroupsCount, opt => opt.MapFrom(src => src.ActiveBrandsCount))
            .ForMember(dest => dest.TotalProductsCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Category.Aggregates.Category, CategoryListItemDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.GroupCount, opt => opt.MapFrom(src => src.Brands.Count))
            .ForMember(dest => dest.ActiveGroupCount, opt => opt.MapFrom(src => src.ActiveBrandsCount))
            .ForMember(dest => dest.TotalProductCount, opt => opt.MapFrom(src => src.TotalProductsCount))
            .ForMember(dest => dest.BrandCount, opt => opt.MapFrom(src => src.Brands.Count))
            .ForMember(dest => dest.ActiveBrandCount, opt => opt.MapFrom(src => src.ActiveBrandsCount))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Category.Aggregates.Category, CategoryTreeDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Category.Aggregates.Category, CategoryWithBrandsDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());
    }
}