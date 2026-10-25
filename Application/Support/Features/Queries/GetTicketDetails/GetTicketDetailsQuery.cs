using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTicketDetails;

/// <summary>
/// The caller's role is resolved from the authenticated principal
/// (<see cref="Application.Common.Interfaces.ICurrentUserService"/>), never from
/// the request payload.
/// </summary>
public sealed record GetTicketDetailsQuery(
    Guid TicketId)
    : IQuery<TicketDto>;