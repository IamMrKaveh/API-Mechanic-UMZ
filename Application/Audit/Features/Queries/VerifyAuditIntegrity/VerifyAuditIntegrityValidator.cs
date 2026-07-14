namespace Application.Audit.Features.Queries.VerifyAuditIntegrity;

public sealed class VerifyAuditIntegrityValidator : AbstractValidator<VerifyAuditIntegrityQuery>
{
    public VerifyAuditIntegrityValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("شناسه لاگ نمی‌تواند خالی باشد.");
    }
}