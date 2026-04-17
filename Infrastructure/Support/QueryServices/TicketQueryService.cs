using Application.Support.Contracts;
using Application.Support.Features.Shared;
using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Support.QueryServices;

public sealed class TicketQueryService(DBContext context) : ITicketQueryService
{
    public async Task<PaginatedResult<TicketDto>> GetAdminTicketsPagedAsync(
        TicketStatus ticketStatus,
        TicketPriority ticketPriority,
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.Tickets
            .AsNoTracking()
            .Where(t => t.Status == ticketStatus && t.Priority == ticketPriority)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto
            {
                Id = t.Id.Value,
                UserId = t.CustomerId.Value,
                CustomerId = t.CustomerId.Value,
                AssignedAgentId = t.AssignedAgentId != null ? t.AssignedAgentId.Value : null,
                Subject = t.Subject,
                Category = t.Category.Value,
                Priority = t.Priority.Value,
                PriorityDisplayName = t.Priority.DisplayName,
                Status = t.Status.Value,
                StatusDisplayName = t.Status.DisplayName,
                MessageCount = t.Messages.Count(),
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
                Subject = t.Subject,
                Category = t.Category.Value,
                Priority = t.Priority.Value,
                PriorityDisplayName = t.Priority.DisplayName,
                Status = t.Status.Value,
                StatusDisplayName = t.Status.DisplayName,
                MessageCount = t.Messages.Count(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastActivityAt = t.LastActivityAt,
                ResolvedAt = t.ResolvedAt,
                Messages = t.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new TicketMessageDto
                    {
                        Id = m.Id.Value,
                        TicketId = m.TicketId.Value,
                        SenderId = m.SenderId.Value,
                        SenderType = m.SenderType.ToString(),
                        Content = m.Content,
                        IsAdminReply = m.SenderType == TicketMessageSenderType.Agent,
                        IsEdited = m.IsEdited,
                        EditedAt = m.EditedAt,
                        SentAt = m.SentAt,
                        CreatedAt = t.CreatedAt
                    }).ToList()
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
        var query = context.Tickets.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(t => t.CustomerId == userId);

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
                Subject = t.Subject,
                Category = t.Category.Value,
                Priority = t.Priority.Value,
                Status = t.Status.Value,
                MessageCount = t.Messages.Count(),
                CreatedAt = t.CreatedAt,
                LastReplyAt = t.LastActivityAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketListItemDto>.Create(items, total, page, pageSize);
    }
}