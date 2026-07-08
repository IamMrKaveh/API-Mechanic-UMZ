using Application.Wallet.Features.Shared;
using Domain.Wallet.Aggregates;
using Mapster;

namespace Application.Wallet.Mapping;

public sealed class WalletTransferMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<WalletTransfer, ConfirmWalletTransferResultDto>()
            .Map(dest => dest.TransferId, src => src.Id.Value)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Amount, src => src.Amount.Amount)
            .Map(dest => dest.CorrelationId, src => src.CorrelationId)
            .Map(dest => dest.CompletedAt, src => src.CompletedAt ?? DateTime.UtcNow)
            .Ignore(dest => dest.RecipientDisplayName);
    }
}