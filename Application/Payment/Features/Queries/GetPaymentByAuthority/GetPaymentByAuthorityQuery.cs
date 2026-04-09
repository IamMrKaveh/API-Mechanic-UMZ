using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentByAuthority;

public record GetPaymentByAuthorityQuery(string Authority) : IRequest<ServiceResult<PaymentTransactionDto?>>;