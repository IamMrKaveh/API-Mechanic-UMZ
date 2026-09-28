using Domain.Support.Aggregates;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Support.Repositories;

public sealed class TicketRepository(DBContext context)
    : RepositoryBase<Ticket, TicketId>(context), ITicketRepository
{
    public async Task<Ticket?> GetByIdWithMessagesAsync(TicketId id, CancellationToken ct = default)
        => await Context.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}