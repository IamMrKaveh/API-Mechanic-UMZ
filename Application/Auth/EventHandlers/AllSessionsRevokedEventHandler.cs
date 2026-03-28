using Domain.Security.Events;
using Domain.Security.Interfaces;

namespace Application.Auth.EventHandlers;

public class AllSessionsRevokedEventHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<AllSessionsRevokedEventHandler> logger) : INotificationHandler<AllSessionsRevokedEvent>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<AllSessionsRevokedEventHandler> _logger = logger;

    public async Task Handle(AllSessionsRevokedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Domain Event: All sessions revoked for user {UserId}.",
            notification.UserId);

        await _sessionRepository.RevokeAllByUserAsync(notification.UserId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}