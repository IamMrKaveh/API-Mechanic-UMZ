namespace Application.Support.Mapping;

public class SupportMappingProfile : Profile
{
    public SupportMappingProfile()
    {
        CreateMap<Domain.Support.Ticket, TicketDto>();

        CreateMap<Domain.Support.Ticket, TicketDetailDto>()
            .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages));

        CreateMap<Domain.Support.TicketMessage, TicketMessageDto>();
    }
}