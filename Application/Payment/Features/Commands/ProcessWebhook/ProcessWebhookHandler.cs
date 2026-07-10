using Domain.Payment.Exceptions;

namespace Application.Payment.Features.Commands.ProcessWebhook;

public class ProcessWebhookHandler(
    IPaymentService paymentService)
    : ICommandHandler<ProcessWebhookCommand>
{
    public async Task<ServiceResult> Handle(ProcessWebhookCommand request, CancellationToken ct)
    {
        try
        {
            await paymentService.ProcessWebhookAsync(request.Authority, request.Status, ct);
            return ServiceResult.Success();
        }
        catch (PaymentTransactionNotFoundException ex)
        {
            return ServiceResult.NotFound(ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}