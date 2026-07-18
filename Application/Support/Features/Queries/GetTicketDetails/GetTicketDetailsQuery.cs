using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed record GetTicketDetailsQuery(
    Guid TicketId,
    bool IsAdmin)
    : IQuery<TicketDto>;