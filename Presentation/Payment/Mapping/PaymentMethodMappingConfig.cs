using Application.Payment.Features.Commands.CreatePaymentMethod;
using Application.Payment.Features.Commands.UpdatePaymentMethod;
using Mapster;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Mapping;

public sealed class PaymentMethodMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreatePaymentMethodRequest, CreatePaymentMethodCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Code, src => src.Code)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IconUrl, src => src.IconUrl)
            .Map(dest => dest.FeeAmount, src => src.FeeAmount)
            .Map(dest => dest.FeePercentage, src => src.FeePercentage)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdatePaymentMethodRequest, UpdatePaymentMethodCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IconUrl, src => src.IconUrl)
            .Map(dest => dest.FeeAmount, src => src.FeeAmount)
            .Map(dest => dest.FeePercentage, src => src.FeePercentage)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Ignore(dest => dest.Id)
            .IgnoreNonMapped(true);
    }
}