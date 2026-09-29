using Application.Common.Validation;

namespace Application.Audit.Features.Queries.GetAuditLogById;

public sealed class GetAuditLogByIdValidator : AbstractValidator<GetAuditLogByIdQuery>
{
    public GetAuditLogByIdValidator()
    {
        this.RuleForRequiredId(x => x.Id, "شناسه لاگ نمی‌تواند خالی باشد.");
    }
}
