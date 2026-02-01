using Application.Common.Interfaces.Persistence.Support;

namespace Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly LedkaContext _context;

    public TicketRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<List<Domain.Support.Ticket>> GetByUserIdAsync(int userId)
    {
        return await _context.Tickets
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Domain.Support.Ticket?> GetByIdAsync(int id, int userId)
    {
        return await _context.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && !t.IsDeleted);
    }

    public async Task AddAsync(Domain.Support.Ticket ticket)
    {
        await _context.Tickets.AddAsync(ticket);
    }

    public async Task AddMessageAsync(Domain.Support.TicketMessage message)
    {
        await _context.TicketMessages.AddAsync(message);
    }

    public void Update(Domain.Support.Ticket ticket)
    {
        _context.Tickets.Update(ticket);
    }
}