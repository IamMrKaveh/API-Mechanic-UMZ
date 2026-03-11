using Domain.Support.Interfaces;

namespace Application.Support.Features.Commands.ReplyToTicket;

public sealed class ReplyToTicketHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<ReplyToTicketCommand, ServiceResult>
{
    private readonly ITicketRepository _ticketRepository = ticketRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        ReplyToTicketCommand request,
        CancellationToken ct)
    {
        var senderId = _currentUserService.UserId!.Value;
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);

        if (ticket is null)
            return ServiceResult.Failure("Ticket not found.");

        ticket.AddReply(senderId, request.Message);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}