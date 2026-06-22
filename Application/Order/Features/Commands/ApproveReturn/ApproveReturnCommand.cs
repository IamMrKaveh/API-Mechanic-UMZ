namespace Application.Order.Features.Commands.ApproveReturn;

public record ApproveReturnCommand(
    Guid OrderId,
    string Reason = "تأیید مرجوعی توسط ادمین")
    : ICommand, IBypassTransactionBehavior;