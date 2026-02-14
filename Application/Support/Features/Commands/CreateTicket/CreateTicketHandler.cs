namespace Application.Support.Features.Commands.CreateTicket;

public sealed class CreateTicketHandler : IRequestHandler<CreateTicketCommand, ServiceResult<int>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTicketHandler> _logger;

    public CreateTicketHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTicketHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = Ticket.Open(
            request.UserId,
            request.Subject,
            request.Priority,
            request.Message);

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket {TicketId} created by user {UserId}", ticket.Id, request.UserId);

        return ServiceResult<int>.Success(ticket.Id);
    }
}