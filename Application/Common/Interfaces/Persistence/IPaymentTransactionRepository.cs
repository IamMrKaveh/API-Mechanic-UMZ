namespace Application.Common.Interfaces.Persistence;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(int id);
    Task<PaymentTransaction?> GetByAuthorityAsync(string authority);
    Task<PaymentTransaction?> GetByAuthorityForUpdateAsync(string authority);
    Task<IEnumerable<PaymentTransaction>> GetByOrderIdAsync(int orderId);
    Task<IEnumerable<PaymentTransaction>> GetPendingTransactionsAsync(DateTime cutoffTime);
    Task<(IEnumerable<PaymentTransaction> Transactions, int TotalCount)> GetPagedAsync(
        int? orderId,
        int? userId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize);
    Task AddAsync(PaymentTransaction transaction);
    void Update(PaymentTransaction transaction);
}