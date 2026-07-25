using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Domain.Discount.Enums;
using Mapster;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Mapping;

public sealed class DiscountMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateDiscountRequest, CreateDiscountCommand>()
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.DiscountType, src => ParseDiscountType(src.DiscountType))
            .Map(dest => dest.Value, src => src.DiscountValue)
            .Map(dest => dest.MaximumDiscountAmount, src => src.MaximumDiscountAmount)
            .Map(dest => dest.UsageLimit, src => src.UsageLimit)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateDiscountRequest, UpdateDiscountCommand>()
            .Map(dest => dest.DiscountType, src => ParseDiscountType(src.DiscountType))
            .Map(dest => dest.Value, src => src.DiscountValue)
            .Map(dest => dest.MaximumDiscountAmount, src => src.MaximumDiscountAmount)
            .Map(dest => dest.UsageLimit, src => src.UsageLimit)
            .Map(dest => dest.StartsAt, src => src.StartsAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Ignore(dest => dest.Id)
            .IgnoreNonMapped(true);
    }

    private static DiscountType ParseDiscountType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DiscountType.Percentage;

        return Enum.TryParse<DiscountType>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : DiscountType.Percentage;
    }
}
