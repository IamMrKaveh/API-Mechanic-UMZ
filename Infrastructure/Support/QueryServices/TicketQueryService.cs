using Application.Support.Contracts;
using Application.Support.Features.Shared;
using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Support.QueryServices;

public sealed class TicketQueryService(DBContext context) : ITicketQueryService
{
    private const string EmptyString = "";

    public async Task<PaginatedResult<TicketDto>> GetAdminTicketsPagedAsync(
        TicketStatus ticketStatus,
        TicketPriority ticketPriority,
        UserId? userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = context.Tickets
            .AsNoTracking()
            .Where(t => t.Status == ticketStatus && t.Priority == ticketPriority);

        if (userId is not null)
        {
            var typedUserId = UserId.From(userId.Value);
            query = query.Where(t => t.CustomerId == typedUserId);
        }

        query = query.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto
            {
                Id = t.Id.Value,
                CustomerId = t.CustomerId.Value,
                AssignedAgentId = t.AssignedAgentId != null ? t.AssignedAgentId.Value : null,
                Subject = t.Subject ?? EmptyString,
                Category = t.Category != null ? t.Category.Value : EmptyString,
                Priority = t.Priority != null ? t.Priority.Value : EmptyString,
                PriorityDisplayName = t.Priority != null ? t.Priority.DisplayName : EmptyString,
                Status = t.Status != null ? t.Status.Value : EmptyString,
                StatusDisplayName = t.Status != null ? t.Status.DisplayName : EmptyString,
                MessageCount = t.Messages != null ? t.Messages.Count() : 0,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastActivityAt = t.LastActivityAt,
                ResolvedAt = t.ResolvedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketDto>.Create(items, total, page, pageSize);
    }

    public async Task<TicketDto?> GetTicketDetailAsync(
        TicketId ticketId, CancellationToken ct = default)
    {
        var ticket = await context.Tickets
            .AsNoTracking()
            .Where(t => t.Id == ticketId)
            .Select(t => new TicketDto
            {
                Id = t.Id.Value,
                UserId = t.CustomerId.Value,
                CustomerId = t.CustomerId.Value,
                AssignedAgentId = t.AssignedAgentId != null ? t.AssignedAgentId.Value : null,
                Subject = t.Subject ?? EmptyString,
                Category = t.Category != null ? t.Category.Value : EmptyString,
                Priority = t.Priority != null ? t.Priority.Value : EmptyString,
                PriorityDisplayName = t.Priority != null ? t.Priority.DisplayName : EmptyString,
                Status = t.Status != null ? t.Status.Value : EmptyString,
                StatusDisplayName = t.Status != null ? t.Status.DisplayName : EmptyString,
                MessageCount = t.Messages != null ? t.Messages.Count() : 0,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastActivityAt = t.LastActivityAt,
                ResolvedAt = t.ResolvedAt,
                Messages = t.Messages != null
                    ? t.Messages
                        .OrderBy(m => m.SentAt)
                        .Select(m => new TicketMessageDto
                        {
                            Id = m.Id.Value,
                            TicketId = m.TicketId.Value,
                            SenderId = m.SenderId.Value,
                            SenderType = m.SenderType.ToString(),
                            Content = m.Content ?? EmptyString,
                            IsAdminReply = m.SenderType == TicketMessageSenderType.Agent,
                            IsEdited = m.IsEdited,
                            EditedAt = m.EditedAt,
                            SentAt = m.SentAt,
                            CreatedAt = t.CreatedAt
                        }).ToList()
                    : new List<TicketMessageDto>()
            })
            .FirstOrDefaultAsync(ct);

        return ticket;
    }

    public async Task<PaginatedResult<TicketListItemDto>> GetTicketsPagedAsync(
        UserId? userId,
        TicketStatus? status,
        TicketPriority? priority,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = context.Tickets.AsNoTracking().AsQueryable();

        if (userId is not null)
        {
            var typedUserId = UserId.From(userId.Value);
            query = query.Where(t => t.CustomerId == typedUserId);
        }

        if (status is not null)
            query = query.Where(t => t.Status == status);

        if (priority is not null)
            query = query.Where(t => t.Priority == priority);

        query = query.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id.Value,
                Subject = t.Subject ?? EmptyString,
                Category = t.Category != null ? t.Category.Value : EmptyString,
                Priority = t.Priority != null ? t.Priority.Value : EmptyString,
                Status = t.Status != null ? t.Status.Value : EmptyString,
                MessageCount = t.Messages != null ? t.Messages.Count() : 0,
                CreatedAt = t.CreatedAt,
                LastReplyAt = t.LastActivityAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketListItemDto>.Create(items, total, page, pageSize);
    }
}
