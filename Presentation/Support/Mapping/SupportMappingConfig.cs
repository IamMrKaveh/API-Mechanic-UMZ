using Application.Support.Features.Commands.CreateTicket;
using Mapster;
using Presentation.Support.Requests;

namespace Presentation.Support.Mapping;

public sealed class SupportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateTicketRequest, CreateTicketCommand>();
    }
}