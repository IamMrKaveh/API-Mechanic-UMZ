using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Mapster;
using Presentation.Support.Requests;

namespace Presentation.Support.Mapping;

public sealed class SupportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateTicketRequest, CreateTicketCommand>()
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Category, src => src.Category)
            .Map(dest => dest.Priority, src => src.Priority)
            .Map(dest => dest.Message, src => src.Message)
            .Ignore(dest => dest.UserId)
            .IgnoreNonMapped(true);
    }
}