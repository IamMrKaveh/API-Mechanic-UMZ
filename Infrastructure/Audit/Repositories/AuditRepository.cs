using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Audit.Repositories;

public class AuditRepository(DBContext context) : IAuditRepository
{
    private readonly DBContext _context = context;

    public async Task AddAuditLogAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AuditLog>> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}