using Application.Common.Results;
using Application.Support.Features.Shared;
using Domain.Support.Aggregates;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed record GetTicketDetailsQuery(
    Ticket Ticket,
    UserId UserId,
    bool IsAdmin) : IRequest<ServiceResult<TicketDetailDto>>;