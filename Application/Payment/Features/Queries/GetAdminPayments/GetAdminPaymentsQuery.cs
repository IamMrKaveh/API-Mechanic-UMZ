namespace Application.Payment.Features.Queries.GetAdminPayments;

public record GetAdminPaymentsQuery(PaymentSearchParams Params) : IRequest<ServiceResult<PaginatedResult<PaymentTransactionDto>>>;