using Application.Common.Results;

namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public record GetPaymentsByOrderQuery(int OrderId) : IRequest<ServiceResult<IEnumerable<PaymentTransactionDto>>>;