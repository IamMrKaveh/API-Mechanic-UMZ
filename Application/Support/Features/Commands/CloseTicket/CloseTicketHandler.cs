using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Support.Interfaces;
using Domain.Support.Services;

namespace Application.Support.Features.Commands.CloseTicket;

public sealed class CloseTicketHandler(
    ITicketRepository ticketRepository,
    TicketDomainService ticketDomainService,
    IUnitOfWork unitOfWork,
    ILogger<CloseTicketHandler> logger) : IRequestHandler<CloseTicketCommand, ServiceResult<bool>>
{
    private readonly ITicketRepository _ticketRepository = ticketRepository;
    private readonly TicketDomainService _ticketDomainService = ticketDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CloseTicketHandler> _logger = logger;

    public async Task<ServiceResult<bool>> Handle(
        CloseTicketCommand request,
        CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdWithMessagesAsync(request.TicketId, ct);
        if (ticket is null)
            return ServiceResult<bool>.NotFound("تیکت پشتیبانی یافت نشد");

        var accessResult = _ticketDomainService.ValidateUserAccess(ticket, request.UserId, request.IsAdmin);
        if (!accessResult.HasAccess)
            return ServiceResult<bool>.Forbidden("شما اجازه دسترسی به این تیکت را ندارید");

        var canCloseResult = _ticketDomainService.ValidateCanClose(ticket);
        if (!canCloseResult.CanClose)
            return ServiceResult<bool>.Validation(canCloseResult.Error!);

        ticket.Close();

        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Ticket {TicketId} closed by user {UserId}", request.TicketId, request.UserId);

        return ServiceResult<bool>.Success(true);
    }
}