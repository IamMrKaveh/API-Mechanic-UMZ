using Application.Common.Validation;

namespace Application.Payment.Features.Commands.ProcessWebhook;

public class ProcessWebhookValidator : AbstractValidator<ProcessWebhookCommand>
{

    public ProcessWebhookValidator()
    {
        RuleFor(x => x.Authority).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Nonce).MaximumLength(200);
    }
}
