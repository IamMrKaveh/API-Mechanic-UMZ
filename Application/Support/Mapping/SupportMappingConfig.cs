using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Shared;
using Domain.Support.Aggregates;
using Domain.Support.Entities;
using Mapster;

namespace Application.Support.Mapping;

public class SupportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ticket, TicketDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.CustomerId.Value)
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Category, src => src.Category.Value)
            .Map(dest => dest.Priority, src => src.Priority.Value)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Ignore(dest => dest.UserFullName)
            .Ignore(dest => dest.Messages)
            .Ignore(dest => dest.ClosedAt);

        config.NewConfig<Ticket, TicketListItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Category, src => src.Category.Value)
            .Map(dest => dest.Priority, src => src.Priority.Value)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.MessageCount, src => src.MessageCount)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastReplyAt, src => src.LastActivityAt);

        config.NewConfig<TicketMessage, TicketMessageDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.SenderId, src => src.SenderId.Value)
            .Map(dest => dest.Content, src => src.Content)
            .Map(dest => dest.IsAdminReply, src => src.IsFromAgent())
            .Map(dest => dest.CreatedAt, src => src.SentAt)
            .Ignore(dest => dest.SenderName);

        config.NewConfig<CreateTicketDto, CreateTicketCommand>()
           .Map(dest => dest.Subject, src => src.Subject)
           .Map(dest => dest.Message, src => src.Message)
           .Map(dest => dest.Priority, src => src.Priority)
           .IgnoreNonMapped(true);

        config.NewConfig<ReplyToTicketDto, ReplyToTicketCommand>()
            .Map(dest => dest.Message, src => src.Message)
            .IgnoreNonMapped(true);

        config.NewConfig<CloseTicketDto, CloseTicketCommand>()
            .Map(dest => dest.Resolution, src => src.Resolution)
            .IgnoreNonMapped(true);
    }
}