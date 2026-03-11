using Domain.Support.Aggregates;
using Domain.Support.Interfaces;

namespace Infrastructure.Support.Repositories;

public class TicketRepository(DBContext context) : ITicketRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        await _context.Tickets.AddAsync(ticket, ct);
    }

    public void Update(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
    }

    public async Task<Ticket?> GetByIdWithMessagesAsync(int id, CancellationToken ct = default)
    {
        return await _context.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}