using Domain.Support.Aggregates;

namespace Application.Support.Mapping;

public class SupportMappingProfile : Profile
{
    public SupportMappingProfile()
    {
        CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.ClosedAt, opt => opt.Ignore());

        CreateMap<Ticket, TicketDetailDto>()
            .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages))
            .ForMember(dest => dest.ClosedAt, opt => opt.Ignore());

        CreateMap<TicketMessage, TicketMessageDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.SenderId))
            .ForMember(dest => dest.UserName, opt => opt.Ignore());
    }
}