namespace Application.Payment.Features.Commands.ExpireStalePayments;

/// <summary>
/// انقضای پرداخت‌های معلق - فراخوانی از Background Job
/// </summary>
public record ExpireStalePaymentsCommand(DateTime CutoffTime) : IRequest<ServiceResult<int>>;