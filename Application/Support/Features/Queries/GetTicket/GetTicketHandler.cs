using Application.Common.Interfaces;
using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;

namespace Application.Support.Features.Queries.GetTicket;

public class GetTicketHandler(
    ITicketQueryService supportQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetTicketQuery, TicketDto>
{
    public async Task<ServiceResult<TicketDto>> Handle(
        GetTicketQuery request,
        CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);

        var ticketResult = await (supportQueryService.GetTicketDetailAsync(ticketId, ct)).OrNotFoundAsync("تیکت یافت نشد.");
        if (ticketResult.IsFailure) return ticketResult.Error;
        var ticket = ticketResult.Value;

        // Owner-or-admin check against the authenticated claims, never against
        // data supplied by the client (SEC-22).
        if (!currentUserService.IsAdmin && ticket.UserId != currentUserService.UserId)
            return ServiceResult<TicketDto>.Forbidden("دسترسی به این تیکت وجود ندارد.");

        return ServiceResult<TicketDto>.Success(ticket);
    }
}
