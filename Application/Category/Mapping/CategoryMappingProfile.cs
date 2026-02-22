namespace Application.Category.Mapping;

public sealed class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Domain.Category.Category, CategoryViewDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64()))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands));

        CreateMap<Domain.Category.Category, CategoryListItemDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64()))
            .ForMember(dest => dest.GroupCount, opt => opt.MapFrom(src => src.Brands.Count))
            .ForMember(dest => dest.ActiveGroupCount, opt => opt.MapFrom(src => src.ActiveBrandsCount))
            .ForMember(dest => dest.TotalProductCount, opt => opt.MapFrom(src => src.TotalProductsCount));

        CreateMap<Domain.Category.Category, CategoryTreeDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands));

        CreateMap<Domain.Category.Category, CategoryWithBrandsDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64()))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands));

        CreateMap<Domain.Category.Category, CategoryHierarchyDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands));
    }
}