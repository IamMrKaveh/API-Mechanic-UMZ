namespace Application.Wallet.Mapping;

public class WalletMappingProfile : Profile
{
    public WalletMappingProfile()
    {
        CreateMap<Domain.Wallet.Wallet, WalletDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.CurrentBalance, opt => opt.MapFrom(src => src.CurrentBalance))
            .ForMember(dest => dest.ReservedBalance, opt => opt.MapFrom(src => src.ReservedBalance))
            .ForMember(dest => dest.AvailableBalance, opt => opt.MapFrom(src => src.AvailableBalance))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Domain.Wallet.Wallet, WalletBalanceResponse>()
            .ConstructUsing(src => new WalletBalanceResponse(
                src.CurrentBalance,
                src.ReservedBalance,
                src.AvailableBalance))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.CurrentBalance))
            .ForMember(dest => dest.Reserved, opt => opt.MapFrom(src => src.ReservedBalance))
            .ForMember(dest => dest.Available, opt => opt.MapFrom(src => src.AvailableBalance));

        CreateMap<Domain.Wallet.WalletLedgerEntry, WalletLedgerEntryDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AmountDelta, opt => opt.MapFrom(src => src.AmountDelta))
            .ForMember(dest => dest.BalanceAfter, opt => opt.MapFrom(src => src.BalanceAfter))
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.TransactionType.ToString()))
            .ForMember(dest => dest.ReferenceType, opt => opt.MapFrom(src => src.ReferenceType.ToString()))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
    }
}