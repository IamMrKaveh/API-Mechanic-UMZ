using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;

namespace Application.Support.Features.Queries.GetTicket;

public class GetTicketHandler(
    ISupportQueryService supportQueryService) : IRequestHandler<GetTicketQuery, ServiceResult<TicketDto>>
{
    public async Task<ServiceResult<TicketDto>> Handle(
        GetTicketQuery request,
        CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);

        var ticket = await supportQueryService.GetTicketDetailAsync(ticketId, ct);

        if (ticket is null)
            return ServiceResult<TicketDto>.NotFound("تیکت یافت نشد.");

        if (!request.IsAdmin && ticket.UserId != request.RequestingUserId)
            return ServiceResult<TicketDto>.Forbidden("دسترسی به این تیکت وجود ندارد.");

        return ServiceResult<TicketDto>.Success(ticket);
    }
}