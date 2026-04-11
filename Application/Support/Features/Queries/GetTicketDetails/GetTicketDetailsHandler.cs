using Application.Support.Features.Shared;
using Domain.Support.Interfaces;
using Domain.Support.Services;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed class GetTicketDetailsHandler(
    ITicketRepository ticketRepository,
    ISupportQueryService ticketQueryService,
    TicketDomainService ticketDomainService)
        : IRequestHandler<GetTicketDetailsQuery, ServiceResult<TicketDto>>
{
    public async Task<ServiceResult<TicketDto>> Handle(
        GetTicketDetailsQuery request,
        CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);
        var userId = UserId.From(request.UserId);

        var ticket = await ticketRepository.GetByIdWithMessagesAsync(ticketId, ct);

        if (ticket is null)
            return ServiceResult<TicketDto>.NotFound("تیکت یافت نشد.");

        var result = ticketDomainService.ValidateUserAccess(ticket, userId, request.IsAdmin);
        if (!result.HasAccess)
            return ServiceResult<TicketDto>.Forbidden("شما دسترسی به این تیکت را ندارید");

        var dto = await ticketQueryService.GetTicketDetailAsync(ticketId, ct);

        if (dto is null)
            return ServiceResult<TicketDto>.NotFound("تیکت یافت نشد.");

        return ServiceResult<TicketDto>.Success(dto);
    }
}