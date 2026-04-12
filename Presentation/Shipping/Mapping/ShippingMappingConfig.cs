using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Mapster;
using Presentation.Shipping.Requests;

namespace Presentation.Shipping.Mapping;

public sealed class ShippingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateShippingRequest, CreateShippingCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.BaseCost, src => src.BaseCost)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.EstimatedDeliveryTime, src => src.EstimatedDeliveryTime)
            .Map(dest => dest.MinDeliveryDays, src => src.MinDeliveryDays)
            .Map(dest => dest.MaxDeliveryDays, src => src.MaxDeliveryDays)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateShippingRequest, UpdateShippingCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.BaseCost, src => src.BaseCost)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.EstimatedDeliveryTime, src => src.EstimatedDeliveryTime)
            .Map(dest => dest.MinDeliveryDays, src => src.MinDeliveryDays)
            .Map(dest => dest.MaxDeliveryDays, src => src.MaxDeliveryDays)
            .Ignore(dest => dest.Id)
            .IgnoreNonMapped(true);
    }
}