namespace Application.Payment.Mapping;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<PaymentTransaction, PaymentTransactionDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.Value));

        CreateMap<PaymentTransaction, PaymentStatusDto>()
            .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.Value))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.DisplayName))
            .ForMember(dest => dest.TimeUntilExpiry, opt => opt.MapFrom(src => src.GetTimeUntilExpiry()));
    }
}