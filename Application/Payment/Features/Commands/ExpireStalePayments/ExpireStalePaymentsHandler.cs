using Domain.Payment.Interfaces;
using Domain.Payment.Services;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Payment.Features.Commands.ExpireStalePayments;

public class ExpireStalePaymentsHandler(
    IPaymentTransactionRepository paymentRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ExpireStalePaymentsCommand, int>
{
    public async Task<ServiceResult<int>> Handle(ExpireStalePaymentsCommand request, CancellationToken ct)
    {
        var cutoff = dateTimeProvider.UtcNow;
        var expiredTransactions = await paymentRepository.GetPendingExpiredTransactionsAsync(cutoff, ct);
        var txList = expiredTransactions.ToList();

        var count = PaymentDomainService.ExpireStaleTransactions(txList);

        foreach (var tx in txList.Where(t => !t.IsPending()))
            paymentRepository.Update(tx);

        return ServiceResult<int>.Success(count);
    }
}