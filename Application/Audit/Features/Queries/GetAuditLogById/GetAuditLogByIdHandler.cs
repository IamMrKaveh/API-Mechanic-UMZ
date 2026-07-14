using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogById;

public sealed class GetAuditLogByIdHandler(IAuditQueryService auditQueryService)
    : IQueryHandler<GetAuditLogByIdQuery, AuditLogDetailDto>
{
    public async Task<ServiceResult<AuditLogDetailDto>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken ct)
    {
        var detail = await auditQueryService.GetByIdAsync(request.Id, ct);

        if (detail is null)
            return ServiceResult<AuditLogDetailDto>.NotFound("لاگ درخواستی یافت نشد.");

        return ServiceResult<AuditLogDetailDto>.Success(detail);
    }
}