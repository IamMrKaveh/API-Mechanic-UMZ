using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.VerifyAuditIntegrity;

public sealed record VerifyAuditIntegrityQuery(Guid Id) : IQuery<AuditIntegrityResultDto>;