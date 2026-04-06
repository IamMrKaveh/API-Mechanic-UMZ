using Application.Common.Results;

namespace Application.Payment.Features.Commands.ProcessWebhook;

public record ProcessWebhookCommand(string Authority, string Status) : IRequest<ServiceResult>;