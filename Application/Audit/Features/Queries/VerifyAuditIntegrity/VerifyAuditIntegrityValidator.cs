using Application.Common.Validation;

namespace Application.Audit.Features.Queries.VerifyAuditIntegrity;

public sealed class VerifyAuditIntegrityValidator : AbstractValidator<VerifyAuditIntegrityQuery>
{
    public VerifyAuditIntegrityValidator()
    {
        this.RuleForRequiredId(x => x.Id, "شناسه لاگ نمی‌تواند خالی باشد.");
    }
}