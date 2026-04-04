using Application.Audit.Contracts;
using Domain.Common.Interfaces;
using Domain.Security.Interfaces;
using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public class UserPhoneChangedEventHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<UserPhoneChangedEventHandler> logger) : INotificationHandler<UserPhoneChangedEvent>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<UserPhoneChangedEventHandler> _logger = logger;

    public async Task Handle(UserPhoneChangedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Domain Event: User {UserId} phone changed from {OldPhone} to {NewPhone}.",
            notification.UserId, notification.OldPhoneNumber, notification.NewPhoneNumber);

        await _sessionRepository.RevokeAllByUserAsync(notification.UserId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogSecurityEventAsync(
            "PhoneNumberChanged",
            $"شماره تلفن کاربر {notification.UserId} از {notification.OldPhoneNumber} به {notification.NewPhoneNumber} تغییر کرد.",
            "system",
            notification.UserId);
    }
}