using Domain.Common.Interfaces;
using Domain.Support.Aggregates;
using Domain.Support.ValueObjects;

namespace Domain.Support.Interfaces;

public interface ITicketRepository : IRepository<Ticket, TicketId>
{
    Task<Ticket?> GetByIdWithMessagesAsync(
        TicketId id,
        CancellationToken ct = default);
}