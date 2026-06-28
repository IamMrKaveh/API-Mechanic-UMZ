using Application.Payment.Features.Shared;
using Domain.Payment.Aggregates;

namespace Application.Payment.Mapping;

public sealed class PaymentMethodMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PaymentMethod, PaymentMethodDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Code, src => src.Code.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IconUrl, src => src.IconUrl)
            .Map(dest => dest.FeeAmount, src => src.Fee.Amount.Amount)
            .Map(dest => dest.FeePercentage, src => src.Fee.Percentage)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<PaymentMethod, PaymentMethodListItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Code, src => src.Code.Value)
            .Map(dest => dest.IconUrl, src => src.IconUrl)
            .Map(dest => dest.FeeAmount, src => src.Fee.Amount.Amount)
            .Map(dest => dest.FeePercentage, src => src.Fee.Percentage)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Map(dest => dest.SortOrder, src => src.SortOrder);
    }
}