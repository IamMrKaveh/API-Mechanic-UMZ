using Domain.Support.Aggregates;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;

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
}