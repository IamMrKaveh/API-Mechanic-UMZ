using Application.Audit.Features.Shared;
using Domain.Audit.Interfaces;
using Domain.Audit.ValueObjects;

namespace Application.Audit.Features.Queries.VerifyAuditIntegrity;

public sealed class VerifyAuditIntegrityHandler(IAuditRepository auditRepository)
    : IQueryHandler<VerifyAuditIntegrityQuery, AuditIntegrityResultDto>
{
    public async Task<ServiceResult<AuditIntegrityResultDto>> Handle(
        VerifyAuditIntegrityQuery request,
        CancellationToken ct)
    {
        var log = await auditRepository.GetByIdAsync(AuditLogId.From(request.Id), ct);

        if (log is null)
            return ServiceResult<AuditIntegrityResultDto>.NotFound("لاگ درخواستی یافت نشد.");

        var storedHash = log.IntegrityHash;
        var expectedHash = log.RecomputeIntegrityHash();
        var isValid = log.VerifyIntegrity();

        var dto = new AuditIntegrityResultDto
        {
            Id = log.Id.Value,
            IsValid = isValid,
            ExpectedHash = expectedHash,
            StoredHash = storedHash,
            VerifiedAt = DateTime.UtcNow
        };

        return ServiceResult<AuditIntegrityResultDto>.Success(dto);
    }
}