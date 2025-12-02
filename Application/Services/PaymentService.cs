namespace Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LedkaContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentGateway gateway,
        IUnitOfWork unitOfWork,
        LedkaContext context,
        ILogger<PaymentService> logger)
    {
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentRequestResultDto> InitiatePaymentAsync(PaymentInitiationDto dto)
    {
        var response = await _gateway.RequestPaymentAsync(dto);

        if (response.IsSuccess)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = dto.OrderId,
                UserId = dto.UserId,
                Amount = dto.Amount,
                Authority = response.Authority!,
                Gateway = _gateway.GatewayName,
                Status = "Pending",
                Description = dto.Description
            };

            _context.PaymentTransactions.Add(transaction);
            await _unitOfWork.SaveChangesAsync();

            return response;
        }

        return new PaymentRequestResultDto
        {
            IsSuccess = false,
            Message = response.Message ?? "Payment request failed"
        };
    }

    public async Task<PaymentVerificationResultDto> VerifyPaymentAsync(string authority, string status)
    {
        var transaction = await _context.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Authority == authority);

        if (transaction == null)
        {
            return new PaymentVerificationResultDto { IsVerified = false, Message = "Transaction not found" };
        }

        if (transaction.Status == "Verified")
        {
            return new PaymentVerificationResultDto { IsVerified = true, RefId = transaction.RefId, Message = "Already verified" };
        }

        if (status != "OK")
        {
            transaction.Status = "Failed";
            await _unitOfWork.SaveChangesAsync();
            return new PaymentVerificationResultDto { IsVerified = false, Message = "Payment failed by user or gateway" };
        }

        var verifyResponse = await _gateway.VerifyPaymentAsync(transaction.Amount, authority);

        if (verifyResponse.IsVerified)
        {
            transaction.Status = "Verified";
            transaction.RefId = verifyResponse.RefId;
            transaction.CardPan = verifyResponse.CardPan;
            transaction.CardHash = verifyResponse.CardHash;
            transaction.Fee = verifyResponse.Fee;

            var order = transaction.Order;
            if (order != null && !order.IsPaid)
            {
                order.IsPaid = true;
                order.OrderStatusId = 2;
                order.PaymentDate = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            return new PaymentVerificationResultDto
            {
                IsVerified = true,
                RefId = verifyResponse.RefId,
                Message = "Payment verified successfully"
            };
        }

        transaction.Status = "VerificationFailed";
        transaction.ErrorMessage = verifyResponse.Message;
        await _unitOfWork.SaveChangesAsync();

        return new PaymentVerificationResultDto
        {
            IsVerified = false,
            Message = "Verification failed"
        };
    }

    public async Task ProcessGatewayWebhookAsync(string gateway, string authority, string status, long? refId)
    {
        await Task.CompletedTask;
    }

    public async Task CleanupAbandonedPaymentsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-20);
        var stuckTransactions = await _context.PaymentTransactions
            .Where(pt => pt.Status == "Pending" && pt.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var tx in stuckTransactions)
        {
            tx.Status = "Expired";
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}