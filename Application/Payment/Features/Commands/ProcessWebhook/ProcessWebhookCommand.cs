namespace Application.Payment.Features.Commands.ProcessWebhook;

public record ProcessWebhookCommand(
    string GatewayName,
    string Authority,
    string Status,
    long? RefId) : IRequest<ServiceResult>;