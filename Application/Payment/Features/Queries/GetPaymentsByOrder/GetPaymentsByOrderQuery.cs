using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public record GetPaymentsByOrderQuery(Guid OrderId) : IRequest<ServiceResult<IEnumerable<PaymentTransactionDto>>>;