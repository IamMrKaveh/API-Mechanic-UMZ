using Application.Payment.Features.Shared;
using Domain.Payment.Aggregates;
using Mapster;

namespace Application.Payment.Mapping;

public class PaymentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PaymentTransaction, PaymentTransactionDto>()
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
}