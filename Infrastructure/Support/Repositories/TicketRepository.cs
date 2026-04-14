using Domain.Support.Aggregates;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Support.Repositories;

public sealed class TicketRepository(DBContext context) : ITicketRepository
{
    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
        => await context.Tickets.AddAsync(ticket, ct);

    public void Update(Ticket ticket)
        => context.Tickets.Update(ticket);

    public async Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken ct = default)
        => await context.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Ticket?> GetByIdWithMessagesAsync(TicketId id, CancellationToken ct = default)
        => await context.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(UserId customerId, CancellationToken ct = default)
    {
        var result = await context.Tickets
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Ticket>> GetByAgentIdAsync(UserId agentId, CancellationToken ct = default)
    {
        var result = await context.Tickets
            .Where(t => t.AssignedAgentId == agentId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Ticket>> GetOpenTicketsAsync(CancellationToken ct = default)
    {
        var result = await context.Tickets
            .Where(t => t.Status == TicketStatus.Open)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Ticket>> GetByStatusAsync(TicketStatus status, CancellationToken ct = default)
    {
        var result = await context.Tickets
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Ticket>> GetByPriorityAsync(TicketPriority priority, CancellationToken ct = default)
    {
        var result = await context.Tickets
            .Where(t => t.Priority == priority)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Ticket>> GetUnassignedTicketsAsync(CancellationToken ct = default)
    {
        var result = await context.Tickets
            .Where(t => t.AssignedAgentId == null && !t.IsClosed)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Ticket>> GetTicketsRequiringAttentionAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var result = await context.Tickets
            .Where(t => !t.IsClosed && t.LastActivityAt < cutoff)
            .OrderBy(t => t.LastActivityAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<int> CountByStatusAsync(TicketStatus status, CancellationToken ct = default)
        => await context.Tickets.CountAsync(t => t.Status == status, ct);

    public async Task<int> CountByCustomerIdAsync(UserId customerId, CancellationToken ct = default)
        => await context.Tickets.CountAsync(t => t.CustomerId == customerId, ct);
}