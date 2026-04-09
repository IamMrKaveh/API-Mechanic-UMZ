using Application.Attribute.Features.Shared;
using Application.Brand.Features.Shared;
using Application.Cart.Features.Shared;
using Application.Category.Features.Shared;
using Application.Discount.Features.Shared;
using Application.Inventory.Features.Shared;
using Application.Media.Features.Shared;
using Application.Notification.Features.Shared;
using Application.Order.Features.Shared;
using Application.Payment.Features.Shared;
using Application.Review.Features.Shared;
using Application.Shipping.Features.Shared;
using Application.Support.Features.Shared;
using Application.User.Features.Shared;
using Application.Variant.Features.Shared;
using Application.Wallet.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Cart.Entities;
using Domain.Discount.Aggregates;
using Domain.Order.Entities;
using Domain.Payment.Aggregates;
using Domain.Review.Aggregates;
using Domain.Security.Aggregates;
using Domain.Support.Aggregates;
using Domain.Support.Entities;
using Domain.User.Entities;
using Domain.Variant.Aggregates;
using Domain.Wallet.Entities;
using Mapster;

namespace Application.Common.Mapping;

public static class MapsterConfig
{
    public static void Configure()
    {
        ConfigureAttributeMappings();
        ConfigureBrandMappings();
        ConfigureCategoryMappings();
        ConfigureCartMappings();
        ConfigureDiscountMappings();
        ConfigureInventoryMappings();
        ConfigureMediaMappings();
        ConfigureNotificationMappings();
        ConfigureOrderMappings();
        ConfigurePaymentMappings();
        ConfigureReviewMappings();
        ConfigureShippingMappings();
        ConfigureSupportMappings();
        ConfigureUserMappings();
        ConfigureVariantMappings();
        ConfigureWalletMappings();
    }

    private static void ConfigureAttributeMappings()
    {
        TypeAdapterConfig<AttributeType, AttributeTypeDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.DisplayName, src => src.DisplayName)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.Values, src => src.Values.Adapt<List<AttributeValueDto>>());

        TypeAdapterConfig<AttributeValue, AttributeValueDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.AttributeTypeId, src => src.AttributeTypeId.Value)
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.DisplayValue, src => src.DisplayValue)
            .Map(dest => dest.HexCode, src => src.HexCode)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsActive, src => src.IsActive);
    }

    private static void ConfigureBrandMappings()
    {
        TypeAdapterConfig<Domain.Brand.Aggregates.Brand, BrandDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.LogoPath, src => src.LogoPath)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        TypeAdapterConfig<Domain.Brand.Aggregates.Brand, BrandDetailDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Ignore(dest => dest.CategoryName)
            .Ignore(dest => dest.ProductCount)
            .Ignore(dest => dest.ActiveProductCount);
    }

    private static void ConfigureCategoryMappings()
    {
        TypeAdapterConfig<Domain.Category.Aggregates.Category, CategoryDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.ParentCategoryId, src => src.ParentCategoryId != null ? src.ParentCategoryId.Value : (Guid?)null)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsRootCategory, src => src.IsRootCategory)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }

    private static void ConfigureCartMappings()
    {
        TypeAdapterConfig<Domain.Cart.Aggregates.Cart, CartDetailDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.UserId != null ? src.UserId.Value : (Guid?)null)
            .Map(dest => dest.GuestToken, src => src.GuestToken != null ? src.GuestToken.Value : null)
            .Map(dest => dest.IsCheckedOut, src => src.IsCheckedOut)
            .Map(dest => dest.TotalPrice, src => src.TotalAmount.Amount)
            .Map(dest => dest.TotalItems, src => src.Items.Sum(i => i.Quantity))
            .Ignore(dest => dest.Items)
            .Ignore(dest => dest.PriceChanges);

        TypeAdapterConfig<CartItem, CartItemDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CartId, src => src.CartId.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.ProductName, src => src.ProductName.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Amount)
            .Map(dest => dest.OriginalPrice, src => src.OriginalPrice.Amount)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.TotalPrice, src => src.TotalPrice.Amount)
            .Map(dest => dest.AddedAt, src => src.AddedAt)
            .Ignore(dest => dest.ProductIcon)
            .Ignore(dest => dest.Attributes);
    }

    private static void ConfigureDiscountMappings()
    {
        TypeAdapterConfig<DiscountCode, DiscountDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.DiscountType, src => src.Value.Type.ToString())
            .Map(dest => dest.DiscountValue, src => src.Value.Amount)
            .Map(dest => dest.MaximumDiscountAmount, src => src.MaximumDiscountAmount != null ? src.MaximumDiscountAmount.Amount : (decimal?)null)
            .Map(dest => dest.UsageLimit, src => src.UsageLimit)
            .Map(dest => dest.UsageCount, src => src.UsageCount)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsExpired, src => src.IsExpired)
            .Map(dest => dest.IsRedeemable, src => src.IsRedeemable)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }

    private static void ConfigureInventoryMappings()
    {
        TypeAdapterConfig<Domain.Inventory.Aggregates.Inventory, InventoryDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.StockQuantity, src => src.StockQuantity)
            .Map(dest => dest.ReservedQuantity, src => src.ReservedQuantity)
            .Map(dest => dest.AvailableQuantity, src => src.AvailableQuantity)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.IsInStock, src => src.IsInStock)
            .Map(dest => dest.IsLowStock, src => src.IsLowStock)
            .Map(dest => dest.LowStockThreshold, src => src.LowStockThreshold)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);
    }

    private static void ConfigureMediaMappings()
    {
        TypeAdapterConfig<Domain.Media.Aggregates.Media, MediaDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.FilePath, src => src.FilePath)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.FileType, src => src.FileType)
            .Map(dest => dest.FileSize, src => src.FileSize)
            .Map(dest => dest.EntityType, src => src.EntityType)
            .Map(dest => dest.EntityId, src => src.EntityId)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsPrimary, src => src.IsPrimary)
            .Map(dest => dest.AltText, src => src.AltText)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }

    private static void ConfigureNotificationMappings()
    {
        TypeAdapterConfig<Domain.Notification.Aggregates.Notification, NotificationDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.UserId.Value)
            .Map(dest => dest.IsRead, src => src.IsRead)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }

    private static void ConfigureOrderMappings()
    {
        TypeAdapterConfig<Domain.Order.Aggregates.Order, OrderDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.OrderNumber, src => src.OrderNumber.Value)
            .Map(dest => dest.UserId, src => src.UserId.Value)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.StatusDisplayName, src => src.Status.DisplayName)
            .Map(dest => dest.SubTotal, src => src.SubTotal.Amount)
            .Map(dest => dest.ShippingCost, src => src.ShippingCost.Amount)
            .Map(dest => dest.DiscountAmount, src => src.DiscountAmount.Amount)
            .Map(dest => dest.FinalAmount, src => src.FinalAmount.Amount)
            .Map(dest => dest.IsPaid, src => src.IsPaid)
            .Map(dest => dest.IsCancelled, src => src.IsCancelled)
            .Map(dest => dest.CancellationReason, src => src.CancellationReason)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemDto>>());

        TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.ProductName, src => src.ProductName)
            .Map(dest => dest.Sku, src => src.Sku)
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Amount)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.TotalPrice, src => src.TotalPrice.Amount);
    }

    private static void ConfigurePaymentMappings()
    {
        TypeAdapterConfig<PaymentTransaction, PaymentTransactionDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.OrderId, src => src.OrderId.Value)
            .Map(dest => dest.Authority, src => src.Authority.Value)
            .Map(dest => dest.Gateway, src => src.Gateway.Value)
            .Map(dest => dest.Amount, src => src.Amount.Amount)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.StatusDisplayName, src => src.Status.DisplayName)
            .Map(dest => dest.RefId, src => src.RefId)
            .Map(dest => dest.IsSuccessful, src => src.IsSuccessful())
            .Map(dest => dest.VerifiedAt, src => src.VerifiedAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }

    private static void ConfigureReviewMappings()
    {
        TypeAdapterConfig<ProductReview, ProductReviewDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.UserId, src => src.UserId.Value)
            .Map(dest => dest.Rating, src => src.Rating.Value)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.Comment, src => src.Comment)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.IsVerifiedPurchase, src => src.IsVerifiedPurchase)
            .Map(dest => dest.LikeCount, src => src.LikeCount)
            .Map(dest => dest.DislikeCount, src => src.DislikeCount)
            .Map(dest => dest.AdminReply, src => src.AdminReply)
            .Map(dest => dest.RepliedAt, src => src.RepliedAt)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Ignore(dest => dest.UserFullName);
    }

    private static void ConfigureShippingMappings()
    {
        TypeAdapterConfig<Domain.Shipping.Aggregates.Shipping, ShippingDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.BaseCost, src => src.BaseCost.Amount)
            .Map(dest => dest.EstimatedDeliveryTime, src => src.EstimatedDeliveryTime)
            .Map(dest => dest.MinDeliveryDays, src => src.DeliveryTime.MinDays)
            .Map(dest => dest.MaxDeliveryDays, src => src.DeliveryTime.MaxDays)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.FreeShippingThreshold, src => src.FreeShipping.IsEnabled ? src.FreeShipping.ThresholdAmount!.Amount : (decimal?)null)
            .Map(dest => dest.MinOrderAmount, src => src.OrderRange.MinOrderAmount != null ? src.OrderRange.MinOrderAmount.Amount : (decimal?)null)
            .Map(dest => dest.MaxOrderAmount, src => src.OrderRange.MaxOrderAmount != null ? src.OrderRange.MaxOrderAmount.Amount : (decimal?)null)
            .Map(dest => dest.MaxWeight, src => src.MaxWeight)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        TypeAdapterConfig<Domain.Shipping.Aggregates.Shipping, ShippingListItemDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.BaseCost, src => src.BaseCost.Amount)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.DeliveryTimeDisplay, src => src.GetDeliveryTimeDisplay());
    }

    private static void ConfigureSupportMappings()
    {
        TypeAdapterConfig<Ticket, TicketDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.CustomerId.Value)
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Category, src => src.Category.Value)
            .Map(dest => dest.Priority, src => src.Priority.Value)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Ignore(dest => dest.UserFullName)
            .Ignore(dest => dest.Messages)
            .Ignore(dest => dest.ClosedAt);

        TypeAdapterConfig<Ticket, TicketListItemDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Category, src => src.Category.Value)
            .Map(dest => dest.Priority, src => src.Priority.Value)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.MessageCount, src => src.MessageCount)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastReplyAt, src => src.LastActivityAt);

        TypeAdapterConfig<TicketMessage, TicketMessageDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.SenderId, src => src.SenderId.Value)
            .Map(dest => dest.Content, src => src.Content)
            .Map(dest => dest.IsAdminReply, src => src.IsFromAgent())
            .Map(dest => dest.CreatedAt, src => src.SentAt)
            .Ignore(dest => dest.SenderName);
    }

    private static void ConfigureUserMappings()
    {
        TypeAdapterConfig<Domain.User.Aggregates.User, UserProfileDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber != null ? src.PhoneNumber.Value : string.Empty)
            .Map(dest => dest.FirstName, src => src.FullName.FirstName)
            .Map(dest => dest.LastName, src => src.FullName.LastName)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsAdmin, src => src.IsAdmin)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastLoginAt, src => src.LastLoginAt)
            .Map(dest => dest.UserAddresses, src => src.Addresses.Adapt<List<UserAddressDto>>());

        TypeAdapterConfig<UserAddress, UserAddressDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.ReceiverName, src => src.ReceiverName)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber.Value)
            .Map(dest => dest.Province, src => src.Province)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.PostalCode, src => src.PostalCode)
            .Map(dest => dest.Latitude, src => src.Latitude)
            .Map(dest => dest.Longitude, src => src.Longitude)
            .Map(dest => dest.IsDefault, src => src.IsDefault);

        TypeAdapterConfig<UserSession, UserSessionDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CreatedByIp, src => src.IpAddress.Value)
            .Map(dest => dest.DeviceInfo, src => src.DeviceInfo.Value)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastActivityAt, src => src.LastActivityAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Ignore(dest => dest.SessionType)
            .Ignore(dest => dest.BrowserInfo)
            .Ignore(dest => dest.IsCurrent);
    }

    private static void ConfigureVariantMappings()
    {
        TypeAdapterConfig<ProductVariant, ProductVariantDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.CompareAtPrice, src => src.CompareAtPrice != null ? src.CompareAtPrice.Amount : (decimal?)null)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDiscounted, src => src.IsDiscounted)
            .Map(dest => dest.DiscountPercentage, src => src.DiscountPercentage)
            .Map(dest => dest.FinalPrice, src => src.Price.Amount)
            .Ignore(dest => dest.StockQuantity);
    }

    private static void ConfigureWalletMappings()
    {
        TypeAdapterConfig<Domain.Wallet.Aggregates.Wallet, WalletDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.OwnerId.Value)
            .Map(dest => dest.CurrentBalance, src => src.Balance.Amount)
            .Map(dest => dest.ReservedBalance, src => src.ReservedBalance.Amount)
            .Map(dest => dest.AvailableBalance, src => src.AvailableBalance.Amount)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        TypeAdapterConfig<WalletLedgerEntry, WalletLedgerEntryDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.WalletId, src => src.WalletId.Value)
            .Map(dest => dest.UserId, src => src.OwnerId.Value)
            .Map(dest => dest.AmountDelta, src => src.Amount.Amount)
            .Map(dest => dest.BalanceAfter, src => src.BalanceAfter.Amount)
            .Map(dest => dest.TransactionType, src => src.TransactionType.ToString())
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.ReferenceId, src => src.ReferenceId)
            .Map(dest => dest.CreatedAt, src => src.OccurredAt);
    }
}