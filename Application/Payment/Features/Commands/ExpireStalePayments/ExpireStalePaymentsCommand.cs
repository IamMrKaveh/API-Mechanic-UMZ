namespace Application.Payment.Features.Commands.ExpireStalePayments;

public record ExpireStalePaymentsCommand : IRequest<ServiceResult<int>>;