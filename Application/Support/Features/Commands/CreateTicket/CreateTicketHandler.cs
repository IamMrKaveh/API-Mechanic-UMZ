using Domain.Support.Aggregates;
using Domain.Support.Interfaces;

namespace Application.Support.Features.Commands.CreateTicket;

public sealed class CreateTicketHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<CreateTicketCommand, ServiceResult<int>>
{
    private readonly ITicketRepository _ticketRepository = ticketRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult<int>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var ticket = Ticket.Create(userId, request.Subject, request.Message);

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(ticket.Id);
    }
}