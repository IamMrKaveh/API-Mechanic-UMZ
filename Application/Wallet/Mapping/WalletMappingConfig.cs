using Application.Wallet.Features.Shared;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Entities;
using Mapster;

namespace Application.Wallet.Mapping;

public class WalletMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Wallet, WalletDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.OwnerId.Value)
            .Map(dest => dest.CurrentBalance, src => src.Balance.Amount)
            .Map(dest => dest.ReservedBalance, src => src.ReservedBalance.Amount)
            .Map(dest => dest.AvailableBalance, src => src.AvailableBalance.Amount)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<WalletLedgerEntry, WalletLedgerEntryDto>()
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