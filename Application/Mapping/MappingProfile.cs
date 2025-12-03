namespace Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

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
            .ForMember(dest => dest.MinPrice, opt => opt.Ignore())
            .ForMember(dest => dest.MaxPrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalStock, opt => opt.Ignore())
            .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
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
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Stock, opt => opt.Ignore());

        CreateMap<Product, AdminProductViewDto>()
            .ForMember(dest => dest.RowVersion,
                opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup,
                opt => opt.MapFrom(src => src.CategoryGroup != null ? new
                {
                    Id = src.CategoryGroup.Id,
                    Name = src.CategoryGroup.Name,
                    CategoryName = src.CategoryGroup.Category != null ? src.CategoryGroup.Category.Name : "N/A"
                } : null));

        CreateMap<Product, PublicProductViewDto>()
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup,
                opt => opt.MapFrom(src => src.CategoryGroup != null ? new
                {
                    Id = src.CategoryGroup.Id,
                    Name = src.CategoryGroup.Name,
                    CategoryName = src.CategoryGroup.Category != null ? src.CategoryGroup.Category.Name : "N/A"
                } : null));

        CreateMap<ProductVariant, ProductVariantResponseDto>()
            .ForMember(dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.Images, opt => opt.Ignore())
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
                        ? (1 - (src.SellingPrice / src.OriginalPrice)) * 100
                        : 0));

        CreateMap<AttributeType, AttributeTypeWithValuesDto>()
            .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.AttributeValues));

        CreateMap<AttributeValue, AttributeValueSimpleDto>();

        CreateMap<AttributeValue, AttributeValueDto>()
            .ConstructUsing(src => new AttributeValueDto(
                src.Id,
                src.AttributeType.Name,
                src.AttributeType.DisplayName,
                src.Value,
                src.DisplayValue,
                src.HexCode));

        CreateMap<Category, CategoryViewDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

        CreateMap<CategoryGroup, CategoryGroupSummaryDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Category, CategoryDetailViewDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
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
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<CategoryUpdateDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RowVersion) ? Convert.FromBase64String(src.RowVersion) : null));

        CreateMap<CategoryGroup, CategoryGroupViewDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

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
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
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
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RowVersion) ? Convert.FromBase64String(src.RowVersion) : null));

        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.UserAddresses, opt => opt.MapFrom(src => src.UserAddresses.Where(a => !a.IsDeleted)));

        CreateMap<UserAddress, UserAddressDto>();
        CreateMap<CreateUserAddressDto, UserAddress>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore());

        CreateMap<UpdateUserAddressDto, UserAddress>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore());

        CreateMap<UpdateProfileDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Media, MediaDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore());

        CreateMap<ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? (src.User.FirstName + " " + src.User.LastName).Trim() : "کاربر ناشناس"));

        CreateMap<Product, ProductSummaryDto>()
            .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src => src.MinPrice))
            .ForMember(dest => dest.PurchasePrice, opt => opt.Ignore())
            .ForMember(dest => dest.Icon, opt => opt.Ignore())
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.TotalStock))
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.TotalStock > 0 || src.Variants.Any(v => v.IsUnlimited)));

        CreateMap<ShippingMethod, ShippingMethodDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

        CreateMap<ShippingMethodCreateDto, ShippingMethod>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Orders, opt => opt.Ignore());

        CreateMap<ShippingMethodUpdateDto, ShippingMethod>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Orders, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));

        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.SellingPrice, opt => opt.Ignore())
            .ForMember(dest => dest.ProductIcon, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.Attributes, opt => opt.Ignore());

        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.ReceiverName))
            .ForMember(dest => dest.UserAddress, opt => opt.Ignore());

        CreateMap<OrderStatus, OrderStatusDto>();

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : null));

        CreateMap<DiscountCode, DiscountCodeDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : null))
            .ForMember(dest => dest.VariantSku, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Sku : null))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? (src.User.FirstName + " " + src.User.LastName).Trim() : null))
            .ForMember(dest => dest.StockAfter, opt => opt.MapFrom(src => src.StockBefore + src.QuantityChange));

        CreateMap<Notification, NotificationDto>();

        CreateMap<AuditLog, AuditLogDto>();
    }
}