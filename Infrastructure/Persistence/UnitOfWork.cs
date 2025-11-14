namespace Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly MechanicContext _context;

    public UnitOfWork(MechanicContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}