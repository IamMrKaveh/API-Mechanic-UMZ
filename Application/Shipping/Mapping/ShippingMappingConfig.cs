using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Shared;
using Domain.Shipping.Aggregates;
using Mapster;

namespace Application.Shipping.Mapping;

public class ShippingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Shipping, ShippingDto>()
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

        config.NewConfig<Shipping, ShippingListItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.BaseCost, src => src.BaseCost.Amount)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.DeliveryTimeDisplay, src => src.GetDeliveryTimeDisplay());

        config.NewConfig<CreateShippingDto, CreateShippingCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.BaseCost, src => src.BaseCost)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.EstimatedDeliveryTime, src => src.EstimatedDeliveryTime)
            .Map(dest => dest.MinDeliveryDays, src => src.MinDeliveryDays)
            .Map(dest => dest.MaxDeliveryDays, src => src.MaxDeliveryDays);

        config.NewConfig<UpdateShippingDto, UpdateShippingCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.BaseCost, src => src.BaseCost)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.EstimatedDeliveryTime, src => src.EstimatedDeliveryTime)
            .Map(dest => dest.MinDeliveryDays, src => src.MinDeliveryDays)
            .Map(dest => dest.MaxDeliveryDays, src => src.MaxDeliveryDays)
            .IgnoreNonMapped(true);
    }
}