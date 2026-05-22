using Domain.Support.Aggregates;
using Domain.Support.ValueObjects;

namespace Domain.Support.Interfaces;

public interface ITicketRepository
{
    Task AddAsync(
        Ticket ticket,
        CancellationToken ct = default);

    void Update(Ticket ticket);

    Task<Ticket?> GetByIdAsync(
        TicketId id,
        CancellationToken ct = default);

    Task<Ticket?> GetByIdWithMessagesAsync(
        TicketId id,
        CancellationToken ct = default);
}