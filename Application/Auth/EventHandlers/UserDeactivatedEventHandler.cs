using Application.Audit.Contracts;
using Domain.Common.Interfaces;
using Domain.Security.Interfaces;
using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public class UserDeactivatedEventHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<UserDeactivatedEventHandler> logger) : INotificationHandler<UserDeactivatedEvent>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<UserDeactivatedEventHandler> _logger = logger;

    public async Task Handle(
        UserDeactivatedEvent notification,
        CancellationToken ct)
    {
        _logger.LogInformation("Domain Event: User {UserId} deactivated.", notification.UserId);

        await _sessionRepository.RevokeAllByUserAsync(notification.UserId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogSecurityEventAsync(
            "AccountDeactivated",
            $"حساب کاربر {notification.UserId} غیرفعال شد و تمام نشست‌ها ابطال شدند.",
            "system",
            notification.UserId.Value);
    }
}