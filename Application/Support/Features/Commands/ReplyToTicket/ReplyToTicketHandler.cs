namespace Application.Support.Features.Commands.ReplyToTicket;

public sealed class ReplyToTicketHandler : IRequestHandler<ReplyToTicketCommand, ServiceResult<bool>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly TicketDomainService _ticketDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReplyToTicketHandler> _logger;

    public ReplyToTicketHandler(
        ITicketRepository ticketRepository,
        TicketDomainService ticketDomainService,
        IUnitOfWork unitOfWork,
        ILogger<ReplyToTicketHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _ticketDomainService = ticketDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> Handle(ReplyToTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            throw new TicketNotFoundException(request.TicketId);

        
        var accessResult = _ticketDomainService.ValidateUserAccess(ticket, request.SenderId, request.IsAdminReply);
        if (!accessResult.HasAccess)
            throw new TicketAccessDeniedException(request.TicketId, request.SenderId);

        
        var canSendResult = _ticketDomainService.ValidateCanSendMessage(ticket);
        if (!canSendResult.CanSend)
            return ServiceResult<bool>.Failure(canSendResult.Error!);

        ticket.AddMessage(request.Message, request.IsAdminReply, request.SenderId);

        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reply added to ticket {TicketId} by {SenderType} {SenderId}",
            request.TicketId,
            request.IsAdminReply ? "admin" : "user",
            request.SenderId);

        return ServiceResult<bool>.Success(true);
    }
}