namespace Infrastructure.Persistence.Interface.Support;

public interface ITicketRepository
{
    Task<List<Ticket>> GetByUserIdAsync(int userId);
    Task<Ticket?> GetByIdAsync(int id, int userId);
    Task AddAsync(Ticket ticket);
    Task AddMessageAsync(TicketMessage message);
    void Update(Ticket ticket);
}