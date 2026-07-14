namespace Application.Audit.Features.Queries.GetAuditLogById;

public sealed class GetAuditLogByIdValidator : AbstractValidator<GetAuditLogByIdQuery>
{
    public GetAuditLogByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("شناسه لاگ نمی‌تواند خالی باشد.");
    }
}