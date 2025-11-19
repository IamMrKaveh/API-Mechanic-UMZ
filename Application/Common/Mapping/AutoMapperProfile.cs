namespace Application.Common.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<CreateProductVariantDto, ProductVariant>();
        CreateMap<Product, PublicProductViewDto>()
            .ForMember(dest => dest.HasMultipleVariants, opt => opt.MapFrom(src => src.Variants.Count > 1))
            .ForMember(dest => dest.TotalStock, opt => opt.MapFrom(src => src.Variants.Sum(v => v.IsUnlimited ? int.MaxValue : v.Stock)));

        CreateMap<Product, AdminProductViewDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => Convert.ToBase64String(src.RowVersion)))
            .IncludeBase<Product, PublicProductViewDto>();

        CreateMap<AttributeType, AttributeTypeWithValuesDto>()
            .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.AttributeValues));

        CreateMap<AttributeValue, AttributeValueDto>()
            .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.AttributeType.Name))
            .ForMember(dest => dest.TypeDisplayName, opt => opt.MapFrom(src => src.AttributeType.DisplayName));

        CreateMap<ProductVariant, ProductVariantResponseDto>()
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.IsUnlimited || src.Stock > 0))
            .ForMember(dest => dest.HasDiscount, opt => opt.MapFrom(src => src.OriginalPrice > src.SellingPrice))
            .ForMember(dest => dest.DiscountPercentage, opt => opt.MapFrom(src => src.OriginalPrice > 0 ? ((src.OriginalPrice - src.SellingPrice) / src.OriginalPrice) * 100 : 0))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src =>
                src.VariantAttributes.ToDictionary(
                    va => va.AttributeValue.AttributeType.Name.ToLower(),
                    va => new AttributeValueDto(
                        va.AttributeValue.Id,
                        va.AttributeValue.AttributeType.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue,
                        va.AttributeValue.HexCode
                    )
                )
            ));

        CreateMap<Media, MediaDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore());

        CreateMap<Category, CategoryViewDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<CategoryCreateDto, Category>();
        CreateMap<CategoryUpdateDto, Category>();

        CreateMap<Category, CategoryDetailViewDto>()
            .IncludeBase<Category, CategoryViewDto>();

        CreateMap<CategoryGroup, CategoryGroupSummaryDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<CategoryGroupCreateDto, CategoryGroup>();
        CreateMap<CategoryGroupUpdateDto, CategoryGroup>();
        CreateMap<CategoryGroup, CategoryGroupViewDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Product, ProductSummaryDto>();

        CreateMap<User, UserProfileDto>();
        CreateMap<UserAddress, UserAddressDto>();

        CreateMap<UpdateProfileDto, User>();
        CreateMap<CreateUserAddressDto, UserAddress>();
        CreateMap<UpdateUserAddressDto, UserAddress>();
    }
}