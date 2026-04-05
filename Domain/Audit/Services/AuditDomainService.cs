using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using SharedKernel.Results;

namespace Domain.Audit.Services;

public sealed class AuditDomainService(IAuditRepository repository)
{
    private readonly IAuditRepository _repository = repository;

    public async Task<Result> RecordAuditAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        if (!auditLog.VerifyIntegrity())
            return Result.Failure(new Error("Audit.Integrity", "عدم تطابق هش لاگ حسابرسی. داده‌ها معتبر نیستند."));

        await _repository.AddAuditLogAsync(auditLog, ct);

        return Result.Success();
    }
}