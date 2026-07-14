using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogById;

public sealed record GetAuditLogByIdQuery(Guid Id) : IQuery<AuditLogDetailDto>;