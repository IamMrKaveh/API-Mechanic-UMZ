using Application.Common.Results;

namespace Application.Order.Features.Commands.ApproveReturn;

public record ApproveReturnCommand(
    Guid OrderId,
    Guid AdminUserId,
    string Reason = "تأیید مرجوعی توسط ادمین") : IRequest<ServiceResult>;