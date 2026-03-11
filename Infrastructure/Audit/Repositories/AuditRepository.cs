using Domain.Audit.Entities;
using Domain.Audit.Interfaces;

namespace Infrastructure.Audit.Repositories;

public class AuditRepository(DBContext context) : IAuditRepository
{
    private readonly DBContext _context = context;

    public async Task AddAuditLogAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}