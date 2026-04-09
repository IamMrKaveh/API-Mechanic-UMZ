using Domain.Support.Aggregates;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

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

    Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(
        UserId customerId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> GetByAgentIdAsync(
        UserId agentId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> GetOpenTicketsAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> GetByStatusAsync(
        TicketStatus status,
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> GetByPriorityAsync(
        TicketPriority priority,
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> GetUnassignedTicketsAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<Ticket>> GetTicketsRequiringAttentionAsync(
        CancellationToken ct = default);

    Task<int> CountByStatusAsync(
        TicketStatus status,
        CancellationToken ct = default);

    Task<int> CountByCustomerIdAsync(
        UserId customerId,
        CancellationToken ct = default);
}