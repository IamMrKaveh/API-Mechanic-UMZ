namespace Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ---------------------------------------------------------
        // Product Mappings
        // ---------------------------------------------------------

        CreateMap<ProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
            .ForMember(dest => dest.MinPrice, opt => opt.Ignore())
            .ForMember(dest => dest.MaxPrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalStock, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RowVersion) ? Convert.FromBase64String(src.RowVersion) : null));

        CreateMap<CreateProductVariantDto, ProductVariant>()
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.VariantAttributes, opt => opt.Ignore())
            .ForMember(dest => dest.InventoryTransactions, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<Product, AdminProductViewDto>()
            .ForMember(dest => dest.RowVersion,
                opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup,
                opt => opt.MapFrom(src => new
                {
                    Id = src.CategoryGroup.Id,
                    Name = src.CategoryGroup.Name,
                    CategoryName = src.CategoryGroup.Category.Name
                }));

        CreateMap<Product, PublicProductViewDto>()
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup,
                opt => opt.MapFrom(src => new
                {
                    Id = src.CategoryGroup.Id,
                    Name = src.CategoryGroup.Name,
                    CategoryName = src.CategoryGroup.Category.Name
                }));

        CreateMap<ProductVariant, ProductVariantResponseDto>()
            .ForMember(dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))

            .ForMember(dest => dest.Attributes,
                opt => opt.MapFrom(src => src.VariantAttributes.ToDictionary(
                    va => va.AttributeValue.AttributeType.Name.ToLower(),
                    va => new AttributeValueDto(
                        va.AttributeValue.Id,
                        va.AttributeValue.AttributeType.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue,
                        va.AttributeValue.HexCode
                    ))))

            .ForMember(dest => dest.DiscountPercentage,
                opt => opt.MapFrom(src =>
                    src.OriginalPrice > src.SellingPrice && src.OriginalPrice > 0
                        ? Math.Round((1 - (src.SellingPrice / src.OriginalPrice)) * 100, 0)
                        : 0));

        CreateMap<AttributeType, AttributeTypeWithValuesDto>()
            .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.AttributeValues));

        CreateMap<AttributeValue, AttributeValueDto>()
            .ConstructUsing(src => new AttributeValueDto(
                src.Id,
                src.AttributeType.Name,
                src.AttributeType.DisplayName,
                src.Value,
                src.DisplayValue,
                src.HexCode));

        // ---------------------------------------------------------
        // Category Mappings
        // ---------------------------------------------------------

        CreateMap<Category, CategoryViewDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));

        CreateMap<CategoryGroup, CategoryGroupSummaryDto>();

        CreateMap<Category, CategoryDetailViewDto>()
            .IncludeBase<Category, CategoryViewDto>();

        CreateMap<CategoryCreateDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<CategoryUpdateDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RowVersion) ? Convert.FromBase64String(src.RowVersion) : null));

        CreateMap<CategoryGroup, CategoryGroupViewDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));

        CreateMap<CategoryGroupCreateDto, CategoryGroup>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<CategoryGroupUpdateDto, CategoryGroup>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RowVersion) ? Convert.FromBase64String(src.RowVersion) : null));

        // ---------------------------------------------------------
        // User Mappings
        // ---------------------------------------------------------

        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.UserAddresses, opt => opt.MapFrom(src => src.UserAddresses));

        CreateMap<UserAddress, UserAddressDto>();
        CreateMap<CreateUserAddressDto, UserAddress>();
        CreateMap<UpdateUserAddressDto, UserAddress>();


        CreateMap<UpdateProfileDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // ---------------------------------------------------------
        // Media Mappings
        // ---------------------------------------------------------

        CreateMap<Media, MediaDto>();

        // ---------------------------------------------------------
        // Review Mappings
        // ---------------------------------------------------------

        CreateMap<ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));

        // ---------------------------------------------------------
        // Product Summary
        // ---------------------------------------------------------

        CreateMap<Product, ProductSummaryDto>()
            .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src => src.MinPrice))
            .ForMember(dest => dest.PurchasePrice, opt => opt.Ignore())
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.TotalStock))
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.TotalStock > 0));

        // ---------------------------------------------------------
        // Shipping Method Mappings
        // ---------------------------------------------------------
        CreateMap<ShippingMethod, ShippingMethodDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));
        CreateMap<ShippingMethodCreateDto, ShippingMethod>();
        CreateMap<ShippingMethodUpdateDto, ShippingMethod>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));
    }
}