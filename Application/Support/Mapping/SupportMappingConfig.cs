using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Shared;
using Domain.Support.Aggregates;
using Domain.Support.Entities;

namespace Application.Support.Mapping;

public class SupportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ticket, TicketDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.CustomerId.Value)
            .Map(dest => dest.CustomerId, src => src.CustomerId.Value)
            .Map(dest => dest.AssignedAgentId, src => src.AssignedAgentId != null ? src.AssignedAgentId.Value : (Guid?)null)
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Category, src => src.Category.Value)
            .Map(dest => dest.Priority, src => src.Priority.Value)
            .Map(dest => dest.PriorityDisplayName, src => src.Priority.DisplayName)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.StatusDisplayName, src => src.Status.DisplayName)
            .Map(dest => dest.MessageCount, src => src.MessageCount)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.LastActivityAt, src => src.LastActivityAt)
            .Map(dest => dest.ResolvedAt, src => src.ResolvedAt)
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
            .Map(dest => dest.TicketId, src => src.TicketId.Value)
            .Map(dest => dest.SenderId, src => src.SenderId.Value)
            .Map(dest => dest.SenderType, src => src.SenderType.ToString())
            .Map(dest => dest.Content, src => src.Content)
            .Map(dest => dest.IsAdminReply, src => src.IsFromAgent())
            .Map(dest => dest.IsEdited, src => src.IsEdited)
            .Map(dest => dest.EditedAt, src => src.EditedAt)
            .Map(dest => dest.SentAt, src => src.SentAt)
            .Map(dest => dest.CreatedAt, src => src.SentAt)
            .Ignore(dest => dest.SenderName);

        config.NewConfig<CreateTicketDto, CreateTicketCommand>()
            .Map(dest => dest.Subject, src => src.Subject)
            .Map(dest => dest.Message, src => src.Message)
            .Map(dest => dest.Priority, src => src.Priority)
            .IgnoreNonMapped(true);

        config.NewConfig<ReplyToTicketDto, ReplyToTicketCommand>()
            .Map(dest => dest.Content, src => src.Message)
            .IgnoreNonMapped(true);
    }
}