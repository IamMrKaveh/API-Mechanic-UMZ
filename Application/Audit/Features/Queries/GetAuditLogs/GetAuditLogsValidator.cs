namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed class GetAuditLogsValidator : AbstractValidator<GetAuditLogsQuery>
{
    private const int MaxPageSize = 200;
    private const int MaxExportRows = 100_000;

    public GetAuditLogsValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"PageSize cannot exceed {MaxPageSize}.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");
    }
}