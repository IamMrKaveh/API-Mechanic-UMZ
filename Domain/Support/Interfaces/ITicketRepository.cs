namespace Domain.Support.Interfaces;

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken ct = default);

    void Update(Ticket ticket);

    Task<Ticket?> GetByIdWithMessagesAsync(int id, CancellationToken ct = default);
}