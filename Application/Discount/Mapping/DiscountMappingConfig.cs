using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.Entities;
using Mapster;

namespace Application.Discount.Mapping;

public class DiscountMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DiscountCode, DiscountDto>()
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

        config.NewConfig<DiscountCode, DiscountCodeDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.DiscountType, src => src.Value.Type.ToString())
            .Map(dest => dest.DiscountValue, src => src.Value.Amount)
            .Map(dest => dest.UsageLimit, src => src.UsageLimit)
            .Map(dest => dest.UsageCount, src => src.UsageCount)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsRedeemable, src => src.IsRedeemable)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        config.NewConfig<DiscountCode, DiscountCodeDetailDto>()
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
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.Restrictions, src => src.Restrictions.Adapt<List<DiscountRestrictionDto>>());

        config.NewConfig<DiscountRestriction, DiscountRestrictionDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.RestrictionType, src => src.RestrictionType.ToString())
            .Map(dest => dest.RestrictionValue, src => src.RestrictionValue);

        config.NewConfig<DiscountCode, DiscountInfoDto>()
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.DiscountType, src => src.Value.Type.ToString())
            .Map(dest => dest.DiscountValue, src => src.Value.Amount)
            .Map(dest => dest.MaximumDiscountAmount, src => src.MaximumDiscountAmount != null ? src.MaximumDiscountAmount.Amount : (decimal?)null)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.IsRedeemable, src => src.IsRedeemable);

        config.NewConfig<CreateDiscountDto, CreateDiscountCommand>()
           .Map(dest => dest.Code, src => src.Code)
           .Map(dest => dest.DiscountType, src => src.DiscountType)
           .Map(dest => dest.Value, src => src.Value)
           .Map(dest => dest.UsageLimit, src => src.UsageLimit)
           .Map(dest => dest.StartsAt, src => src.StartsAt)
           .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
           .Map(dest => dest.IsActive, src => src.IsActive);

        config.NewConfig<UpdateDiscountDto, UpdateDiscountCommand>()
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.UsageLimit, src => src.UsageLimit)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .IgnoreNonMapped(true);
    }
}