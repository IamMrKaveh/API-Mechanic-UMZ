using Application.Support.Contracts;
using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Support.QueryServices;

public sealed class TicketQueryService(DBContext context) : ITicketQueryService
{
    public async Task<PaginatedResult<TicketDto>> GetCustomerTicketsAsync(
        UserId customerId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Tickets
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto
            {
                Id = t.Id.Value,
                Subject = t.Subject,
                Status = t.Status.Value,
                StatusDisplayName = t.Status.DisplayName,
                Priority = t.Priority.Value,
                PriorityDisplayName = t.Priority.DisplayName,
                Category = t.Category.Value,
                CustomerId = t.CustomerId.Value,
                AssignedAgentId = t.AssignedAgentId != null ? t.AssignedAgentId.Value : (Guid?)null,
                MessageCount = t.Messages.Count(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastActivityAt = t.LastActivityAt,
                ResolvedAt = t.ResolvedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketDto>.Create(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<TicketDto>> GetAllTicketsAsync(
        int page,
        int pageSize,
        string? status = null,
        CancellationToken ct = default)
    {
        var query = context.Tickets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status.Value == status);

        query = query.OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto
            {
                Id = t.Id.Value,
                Subject = t.Subject,
                Status = t.Status.Value,
                StatusDisplayName = t.Status.DisplayName,
                Priority = t.Priority.Value,
                PriorityDisplayName = t.Priority.DisplayName,
                Category = t.Category.Value,
                CustomerId = t.CustomerId.Value,
                AssignedAgentId = t.AssignedAgentId != null ? t.AssignedAgentId.Value : (Guid?)null,
                MessageCount = t.Messages.Count(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastActivityAt = t.LastActivityAt,
                ResolvedAt = t.ResolvedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketDto>.Create(items, total, page, pageSize);
    }

    public async Task<TicketDetailDto?> GetTicketDetailAsync(
        TicketId ticketId,
        CancellationToken ct = default)
    {
        var ticket = await context.Tickets
            .AsNoTracking()
            .Where(t => t.Id == ticketId)
            .Select(t => new TicketDetailDto
            {
                Id = t.Id.Value,
                Subject = t.Subject,
                Status = t.Status.Value,
                StatusDisplayName = t.Status.DisplayName,
                Priority = t.Priority.Value,
                PriorityDisplayName = t.Priority.DisplayName,
                Category = t.Category.Value,
                CustomerId = t.CustomerId.Value,
                AssignedAgentId = t.AssignedAgentId != null ? t.AssignedAgentId.Value : (Guid?)null,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastActivityAt = t.LastActivityAt,
                ResolvedAt = t.ResolvedAt,
                Messages = t.Messages.OrderBy(m => m.SentAt).Select(m => new TicketMessageDto
                {
                    Id = m.Id.Value,
                    TicketId = m.TicketId.Value,
                    SenderId = m.SenderId.Value,
                    SenderType = m.SenderType.ToString(),
                    Content = m.Content,
                    IsEdited = m.IsEdited,
                    EditedAt = m.EditedAt,
                    SentAt = m.SentAt
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        return ticket;
    }
}