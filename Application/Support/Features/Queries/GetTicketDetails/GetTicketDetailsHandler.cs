using Application.Common.Models;
using Domain.Support.Interfaces;

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
        var ticket = await _ticketRepository.GetByIdWithMessagesAsync(request.TicketId, ct)
            ?? throw new TicketNotFoundException(request.TicketId);
        var (HasAccess, _) = _ticketDomainService.ValidateUserAccess(ticket, request.UserId, request.IsAdmin);
        if (!HasAccess)
            throw new TicketAccessDeniedException(request.TicketId, request.UserId);

        var dto = await _ticketQueryService.GetTicketDetailAsync(request.TicketId, ct);

        if (dto == null)
            return ServiceResult<TicketDetailDto>.Failure("تیکت یافت نشد.", 404);

        return ServiceResult<TicketDetailDto>.Success(dto);
    }
}