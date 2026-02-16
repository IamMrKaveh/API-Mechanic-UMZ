using Domain.Attribute.Entities;
using Domain.Review;

namespace Infrastructure.Persistence.Mapping;

public class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        CreateMap<CreateProductVariantDto, ProductVariant>()
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.Stock))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.VariantAttributes, opt => opt.Ignore())
            .ForMember(dest => dest.InventoryTransactions, opt => opt.Ignore())
            .ForMember(dest => dest.ProductVariantShippingMethods, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<ProductVariant, ProductVariantResponseDto>()
            .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.IsInStock,
                opt => opt.MapFrom(src => src.IsUnlimited || src.StockQuantity > 0))
            .ForMember(dest => dest.HasDiscount,
                opt => opt.MapFrom(src => src.OriginalPrice > src.SellingPrice))
            .ForMember(dest => dest.DiscountPercentage,
                opt => opt.MapFrom(src =>
                    src.OriginalPrice > 0
                        ? Math.Round(
                            (src.OriginalPrice - src.SellingPrice) / src.OriginalPrice * 100,
                            2)
                        : 0
                ))
            .ForMember(dest => dest.Attributes, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null))
            .ForMember(dest => dest.EnabledShippingMethodIds,
                opt => opt.MapFrom(src =>
                    src.ProductVariantShippingMethods
                        .Where(sm => sm.IsActive)
                        .Select(sm => sm.ShippingMethodId)
                        .ToList()
                ));

        CreateMap<ProductDto, Domain.Product.Product>()
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
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.RowVersion)
                ? Convert.FromBase64String(src.RowVersion)
                : null));

        CreateMap<Domain.Product.Product, AdminProductViewDto>()
            .ForMember(
                dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null
                )
            )
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup,
                opt => opt.MapFrom(src => src.CategoryGroup != null ? new
                {
                    src.CategoryGroup.Id,
                    src.CategoryGroup.Name,
                    CategoryName = src.CategoryGroup.Category != null ? src.CategoryGroup.Category.Name : "N/A"
                } : null));

        CreateMap<Domain.Product.Product, PublicProductViewDto>()
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroup,
                opt => opt.MapFrom(src => src.CategoryGroup != null ? new
                {
                    src.CategoryGroup.Id,
                    src.CategoryGroup.Name,
                    CategoryName = src.CategoryGroup.Category != null ? src.CategoryGroup.Category.Name : "N/A"
                } : null));

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
            .ForMember(
                dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null
                )
            );

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
            .ForMember(
                dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null
                )
            );

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

        CreateMap<Domain.User.User, UserProfileDto>()
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

        CreateMap<UpdateProfileDto, Domain.User.User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Domain.Media.Media, MediaDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore());

        CreateMap<ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? (src.User.FirstName + " " + src.User.LastName).Trim() : "کاربر ناشناس"));

        CreateMap<Domain.Product.Product, ProductSummaryDto>()
            .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src => src.MinPrice))
            .ForMember(dest => dest.PurchasePrice, opt => opt.Ignore())
            .ForMember(dest => dest.Icon, opt => opt.Ignore())
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.TotalStock))
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.TotalStock > 0 || src.Variants.Any(v => v.IsUnlimited)));

        CreateMap<ShippingMethod, ShippingMethodDto>()
            .ForMember(
                dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null
                )
            );

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
            .ForMember(
                dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null
                )
            )
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.SellingPrice, opt => opt.Ignore())
            .ForMember(dest => dest.ProductIcon, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.Attributes, opt => opt.Ignore());

        CreateMap<Domain.Order.Order, OrderDto>()
            .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.ReceiverName))
            .ForMember(dest => dest.UserAddress, opt => opt.Ignore());

        CreateMap<OrderStatus, OrderStatusDto>();

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : null));

        CreateMap<DiscountCode, DiscountCodeDto>()
            .ForMember(
                dest => dest.RowVersion,
                opt => opt.MapFrom(src =>
                    src.RowVersion != null
                        ? Convert.ToBase64String(src.RowVersion)
                        : null
                )
            );

        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : null))
            .ForMember(dest => dest.VariantSku, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Sku : null))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? (src.User.FirstName + " " + src.User.LastName).Trim() : null))
            .ForMember(dest => dest.StockAfter, opt => opt.MapFrom(src => src.StockBefore + src.QuantityChange));

        CreateMap<Domain.Notification.Notification, NotificationDto>();

        CreateMap<AuditLog, AuditDtos>();

        CreateMap<Ticket, TicketDto>();

        CreateMap<Ticket, TicketDetailDto>()
            .ForMember(dest => dest.Messages, opt => opt.MapFrom(src =>
                src.Messages.OrderBy(m => m.CreatedAt)));

        CreateMap<TicketMessage, TicketMessageDto>();

        CreateMap<Domain.Notification.Notification, NotificationDto>();
    }
}