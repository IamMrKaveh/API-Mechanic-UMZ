using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTicket;

// The requester identity (user id and admin role) intentionally does NOT travel
// in the query: it is resolved from the authenticated claims by the handler
// (ICurrentUserService), so a client can never elevate itself via the payload.
public record GetTicketQuery(Guid TicketId)
    : IQuery<TicketDto>;