namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed class GetTicketDetailsHandler
    : IRequestHandler<GetTicketDetailsQuery, ServiceResult<TicketDetailDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly TicketDomainService _ticketDomainService;

    public GetTicketDetailsHandler(
        ITicketRepository ticketRepository,
        TicketDomainService ticketDomainService)
    {
        _ticketRepository = ticketRepository;
        _ticketDomainService = ticketDomainService;
    }

    public async Task<ServiceResult<TicketDetailDto>> Handle(
        GetTicketDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            throw new TicketNotFoundException(request.TicketId);

        var accessResult = _ticketDomainService.ValidateUserAccess(ticket, request.UserId, request.IsAdmin);
        if (!accessResult.HasAccess)
            throw new TicketAccessDeniedException(request.TicketId, request.UserId);

        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Messages = ticket.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TicketMessageDto
                {
                    Id = m.Id,
                    Message = m.Message,
                    IsAdminResponse = m.IsAdminResponse,
                    CreatedAt = m.CreatedAt
                })
                .ToList()
        };

        return ServiceResult<TicketDetailDto>.Success(dto);
    }
}