using Application.Common.Results;
using Application.Support.Contracts;
using Application.Support.Features.Shared;
using Domain.Support.Exceptions;
using Domain.Support.Interfaces;
using Domain.Support.Services;

namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed class GetTicketDetailsHandler(
    ITicketRepository ticketRepository,
    ITicketQueryService ticketQueryService,
    TicketDomainService ticketDomainService)
        : IRequestHandler<GetTicketDetailsQuery, ServiceResult<TicketDetailDto>>
{
    private readonly ITicketRepository _ticketRepository = ticketRepository;
    private readonly ITicketQueryService _ticketQueryService = ticketQueryService;
    private readonly TicketDomainService _ticketDomainService = ticketDomainService;

    public async Task<ServiceResult<TicketDetailDto>> Handle(
        GetTicketDetailsQuery request,
        CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdWithMessagesAsync(request.Ticket, ct)
            ?? throw new TicketNotFoundException(request.Ticket);

        if (ticket is null)
            return ServiceResult<TicketDetailDto>.NotFound("تیکت یافت نشد.");

        var (HasAccess, _) = _ticketDomainService.ValidateUserAccess(ticket, request.UserId, request.IsAdmin);
        if (!HasAccess)
            throw new TicketAccessDeniedException(request.Ticket, request.UserId);

        var dto = await _ticketQueryService.GetTicketDetailAsync(request.Ticket, ct);

        if (dto is null)
            return ServiceResult<TicketDetailDto>.NotFound("تیکت یافت نشد.");

        return ServiceResult<TicketDetailDto>.Success(dto);
    }
}