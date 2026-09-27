using Application.Support.Features.Shared;
using Domain.Support.Interfaces;
using Domain.Support.Services;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed class GetTicketDetailsHandler(
    ITicketRepository ticketRepository,
    ITicketQueryService ticketQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetTicketDetailsQuery, TicketDto>
{
    public async Task<ServiceResult<TicketDto>> Handle(
        GetTicketDetailsQuery request,
        CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);
        var userId = UserId.From(currentUserService.UserId!.Value);

        var ticketResult = await (ticketRepository.GetByIdWithMessagesAsync(ticketId, ct)).OrNotFoundAsync("تیکت یافت نشد.");
        if (ticketResult.IsFailure) return ticketResult.Error;
        var ticket = ticketResult.Value;

        var result = TicketDomainService.ValidateUserAccess(ticket, userId, request.IsAdmin);
        if (!result.HasAccess)
            return ServiceResult<TicketDto>.Forbidden("شما دسترسی به این تیکت را ندارید");

        var dtoResult = await (ticketQueryService.GetTicketDetailAsync(ticketId, ct)).OrNotFoundAsync("تیکت یافت نشد.");
        if (dtoResult.IsFailure) return dtoResult.Error;
        var dto = dtoResult.Value;

        return ServiceResult<TicketDto>.Success(dto);
    }
}
