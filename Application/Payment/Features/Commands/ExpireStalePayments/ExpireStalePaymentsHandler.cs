using Domain.Payment.Interfaces;
using Domain.Payment.Services;

namespace Application.Payment.Features.Commands.ExpireStalePayments;

public class ExpireStalePaymentsHandler(
    IPaymentTransactionRepository paymentRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ExpireStalePaymentsCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(ExpireStalePaymentsCommand request, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow;
        var expiredTransactions = await paymentRepository.GetPendingExpiredTransactionsAsync(cutoff, ct);
        var txList = expiredTransactions.ToList();

        var count = PaymentDomainService.ExpireStaleTransactions(txList);

        foreach (var tx in txList.Where(t => !t.IsPending()))
            paymentRepository.Update(tx);

        if (count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<int>.Success(count);
    }
}