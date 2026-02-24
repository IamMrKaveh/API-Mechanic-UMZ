namespace Application.Support.Features.Commands.CloseTicket;

public sealed class CloseTicketHandler : IRequestHandler<CloseTicketCommand, ServiceResult<bool>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly TicketDomainService _ticketDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CloseTicketHandler> _logger;

    public CloseTicketHandler(
        ITicketRepository ticketRepository,
        TicketDomainService ticketDomainService,
        IUnitOfWork unitOfWork,
        ILogger<CloseTicketHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _ticketDomainService = ticketDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            throw new TicketNotFoundException(request.TicketId);

        var accessResult = _ticketDomainService.ValidateUserAccess(ticket, request.UserId, request.IsAdmin);
        if (!accessResult.HasAccess)
            throw new TicketAccessDeniedException(request.TicketId, request.UserId);

        var canCloseResult = _ticketDomainService.ValidateCanClose(ticket);
        if (!canCloseResult.CanClose)
            return ServiceResult<bool>.Failure(canCloseResult.Error!);

        ticket.Close();

        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket {TicketId} closed by user {UserId}", request.TicketId, request.UserId);

        return ServiceResult<bool>.Success(true);
    }
}