using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogById;

public sealed class GetAuditLogByIdHandler(IAuditQueryService auditQueryService)
    : IQueryHandler<GetAuditLogByIdQuery, AuditLogDetailDto>
{
    public async Task<ServiceResult<AuditLogDetailDto>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken ct)
    {
        if (request.Id == Guid.Empty)
            return ServiceResult<AuditLogDetailDto>.NotFound("لاگ درخواستی یافت نشد.");

        var detailResult = await (auditQueryService.GetByIdAsync(request.Id, ct)).OrNotFoundAsync("لاگ درخواستی یافت نشد.");
        if (detailResult.IsFailure) return detailResult.Error;
        var detail = detailResult.Value;

        return ServiceResult<AuditLogDetailDto>.Success(detail);
    }
}
