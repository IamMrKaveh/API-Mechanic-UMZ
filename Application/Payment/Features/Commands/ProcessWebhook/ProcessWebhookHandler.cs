namespace Application.Payment.Features.Commands.ProcessWebhook;

public class ProcessWebhookHandler(
    IPaymentService paymentService)
    : ICommandHandler<ProcessWebhookCommand>
{
    public Task<ServiceResult> Handle(ProcessWebhookCommand request, CancellationToken ct)
        => paymentService.ProcessWebhookAsync(request.Authority, request.Status, ct);
}