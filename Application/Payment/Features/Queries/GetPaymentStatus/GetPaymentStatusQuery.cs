namespace Application.Payment.Features.Queries.GetPaymentStatus;

public record GetPaymentStatusQuery(string Authority) : IRequest<ServiceResult<PaymentStatusDto>>;