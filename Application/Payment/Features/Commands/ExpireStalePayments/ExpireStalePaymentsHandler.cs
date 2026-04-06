using Application.Common.Results;
using Domain.Payment.Interfaces;
using Domain.Payment.Services;
using Domain.Common.Interfaces;

namespace Application.Payment.Features.Commands.ExpireStalePayments;

public class ExpireStalePaymentsHandler(
    IPaymentTransactionRepository paymentRepository,
    IUnitOfWork unitOfWork,
    ILogger<ExpireStalePaymentsHandler> logger) : IRequestHandler<ExpireStalePaymentsCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(ExpireStalePaymentsCommand request, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow;
        var expiredTransactions = await paymentRepository.GetPendingExpiredTransactionsAsync(cutoff, ct);

        var txList = expiredTransactions.ToList();
        var count = PaymentDomainService.ExpireStaleTransactions(txList);

        foreach (var tx in txList.Where(t => t.IsSuccessful()))
            paymentRepository.Update(tx);

        if (count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("{Count} stale payments expired", count);
        return ServiceResult<int>.Success(count);
    }
}