namespace Infrastructure.Order.QueryServices;

public class OrderStatusQueryService(DBContext context) : IOrderStatusQueryService
{
    private readonly DBContext _context = context;

    public async Task<IEnumerable<OrderStatusDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.OrderStatuses
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                IsDeleted = s.IsDeleted
            })
            .ToListAsync(ct);
    }

    public async Task<OrderStatusDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                IsDeleted = s.IsDeleted
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<OrderStatusDto?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Name == name)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                IsDeleted = s.IsDeleted
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<OrderStatusDto?> GetDefaultStatusAsync(CancellationToken ct = default)
    {
        return await _context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.IsDefault && !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                IsDeleted = s.IsDeleted
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<OrderStatusDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.OrderStatuses
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                IsDeleted = s.IsDeleted
            })
            .ToListAsync(ct);
    }
}