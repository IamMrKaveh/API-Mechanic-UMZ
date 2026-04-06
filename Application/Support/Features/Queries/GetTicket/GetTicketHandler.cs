using Application.Common.Results;
using Application.Support.Contracts;
using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTicket;

public class GetTicketHandler(
    ISupportQueryService supportQueryService) : IRequestHandler<GetTicketQuery, ServiceResult<TicketDto>>
{
    private readonly ISupportQueryService _supportQueryService = supportQueryService;

    public async Task<ServiceResult<TicketDto>> Handle(
        GetTicketQuery request,
        CancellationToken ct)
    {
        var ticket = await _supportQueryService.GetTicketDetailAsync(request.TicketId, ct);
        if (ticket is null)
            return ServiceResult<TicketDto>.NotFound("تیکت یافت نشد.");

        if (!request.IsAdmin && ticket.UserId != request.RequestingUserId)
            return ServiceResult<TicketDto>.Forbidden("دسترسی به این تیکت وجود ندارد.");

        return ServiceResult<TicketDto>.Success(ticket);
    }
}