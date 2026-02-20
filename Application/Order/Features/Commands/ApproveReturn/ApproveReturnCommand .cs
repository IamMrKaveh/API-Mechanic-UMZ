namespace Application.Order.Features.Commands.ApproveReturn;

/// <summary>
/// تأیید مرجوعی توسط ادمین و بازگشت موجودی به انبار
/// </summary>
public record ApproveReturnCommand : IRequest<ServiceResult>
{
    public int OrderId { get; init; }
    public int AdminUserId { get; init; }
    public string Reason { get; init; } = "تأیید مرجوعی توسط ادمین";
}