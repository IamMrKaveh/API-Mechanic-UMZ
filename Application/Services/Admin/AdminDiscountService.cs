namespace Application.Services;

public class AdminDiscountService : IAdminDiscountService
{
    private readonly LedkaContext _context;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminDiscountService> _logger;

    public AdminDiscountService(
        LedkaContext context,
        IHtmlSanitizer htmlSanitizer,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<AdminDiscountService> logger)
    {
        _context = context;
        _htmlSanitizer = htmlSanitizer;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResultDto<DiscountCodeDto>>> GetDiscountsAsync(bool includeExpired, int page, int pageSize)
    {
        var query = _context.DiscountCodes.AsQueryable();

        if (!includeExpired)
        {
            query = query.Where(d => d.IsActive && (!d.ExpiresAt.HasValue || d.ExpiresAt > DateTime.UtcNow));
        }

        var totalItems = await query.CountAsync();

        var discounts = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DiscountCodeDto
            {
                Id = d.Id,
                Code = d.Code,
                Percentage = d.Percentage,
                MaxDiscountAmount = d.MaxDiscountAmount,
                MinOrderAmount = d.MinOrderAmount,
                UsageLimit = d.UsageLimit,
                UsedCount = d.UsedCount,
                IsActive = d.IsActive,
                ExpiresAt = d.ExpiresAt,
                CreatedAt = d.CreatedAt,
                RowVersion = d.RowVersion != null ? Convert.ToBase64String(d.RowVersion) : null
            })
            .ToListAsync();

        var result = PagedResultDto<DiscountCodeDto>.Create(discounts, totalItems, page, pageSize);
        return ServiceResult<PagedResultDto<DiscountCodeDto>>.Ok(result);
    }

    public async Task<ServiceResult<DiscountCodeDetailDto?>> GetDiscountByIdAsync(int id)
    {
        var discount = await _context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages.OrderByDescending(u => u.UsedAt).Take(10))
                .ThenInclude(u => u.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discount == null)
        {
            return ServiceResult<DiscountCodeDetailDto?>.Fail("Discount not found");
        }

        var dto = new DiscountCodeDetailDto
        {
            Id = discount.Id,
            Code = discount.Code,
            Percentage = discount.Percentage,
            MaxDiscountAmount = discount.MaxDiscountAmount,
            MinOrderAmount = discount.MinOrderAmount,
            UsageLimit = discount.UsageLimit,
            UsedCount = discount.UsedCount,
            IsActive = discount.IsActive,
            ExpiresAt = discount.ExpiresAt,
            CreatedAt = discount.CreatedAt,
            RowVersion = discount.RowVersion != null ? Convert.ToBase64String(discount.RowVersion) : null,
            Restrictions = discount.Restrictions.Select(r => new DiscountRestrictionDto
            {
                Id = r.Id,
                RestrictionType = r.RestrictionType,
                EntityId = r.EntityId
            }),
            RecentUsages = discount.Usages.Select(u => new DiscountUsageDto
            {
                Id = u.Id,
                UserId = u.UserId,
                UserName = u.User.FirstName + " " + u.User.LastName,
                OrderId = u.OrderId,
                DiscountAmount = u.DiscountAmount,
                UsedAt = u.UsedAt,
                IsConfirmed = u.IsConfirmed
            })
        };

        return ServiceResult<DiscountCodeDetailDto?>.Ok(dto);
    }

    public async Task<ServiceResult<DiscountCodeDto>> CreateDiscountAsync(CreateDiscountDto dto)
    {
        var sanitizedCode = _htmlSanitizer.Sanitize(dto.Code.ToUpper().Trim());

        if (await _context.DiscountCodes.AnyAsync(d => d.Code.ToLower() == sanitizedCode.ToLower()))
        {
            return ServiceResult<DiscountCodeDto>.Fail("A discount with this code already exists");
        }

        var discount = new DiscountCode
        {
            Code = sanitizedCode,
            Percentage = dto.Percentage,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MinOrderAmount = dto.MinOrderAmount,
            UsageLimit = dto.UsageLimit,
            UsedCount = 0,
            IsActive = true,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Restrictions != null)
        {
            foreach (var restriction in dto.Restrictions)
            {
                discount.Restrictions.Add(new DiscountRestriction
                {
                    RestrictionType = restriction.RestrictionType,
                    EntityId = restriction.EntityId
                });
            }
        }

        _context.DiscountCodes.Add(discount);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAdminEventAsync(
            "CreateDiscount",
            _currentUserService.UserId ?? 0,
            $"Created discount code: {discount.Code}",
            _currentUserService.IpAddress);

        _logger.LogInformation("Discount code created: {Code}", discount.Code);

        var resultDto = new DiscountCodeDto
        {
            Id = discount.Id,
            Code = discount.Code,
            Percentage = discount.Percentage,
            MaxDiscountAmount = discount.MaxDiscountAmount,
            MinOrderAmount = discount.MinOrderAmount,
            UsageLimit = discount.UsageLimit,
            UsedCount = discount.UsedCount,
            IsActive = discount.IsActive,
            ExpiresAt = discount.ExpiresAt,
            CreatedAt = discount.CreatedAt
        };

        return ServiceResult<DiscountCodeDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateDiscountAsync(int id, UpdateDiscountDto dto)
    {
        var discount = await _context.DiscountCodes.FindAsync(id);

        if (discount == null)
        {
            return ServiceResult.Fail("Discount not found");
        }

        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            _context.Entry(discount).Property(d => d.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);
        }

        if (dto.Percentage.HasValue)
            discount.Percentage = dto.Percentage.Value;

        if (dto.MaxDiscountAmount.HasValue)
            discount.MaxDiscountAmount = dto.MaxDiscountAmount;

        if (dto.MinOrderAmount.HasValue)
            discount.MinOrderAmount = dto.MinOrderAmount;

        if (dto.UsageLimit.HasValue)
            discount.UsageLimit = dto.UsageLimit;

        if (dto.IsActive.HasValue)
            discount.IsActive = dto.IsActive.Value;

        if (dto.ExpiresAt.HasValue)
            discount.ExpiresAt = dto.ExpiresAt;

        discount.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAdminEventAsync(
                "UpdateDiscount",
                _currentUserService.UserId ?? 0,
                $"Updated discount code: {discount.Code}",
                _currentUserService.IpAddress);

            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This record was modified by another user.  Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> DeleteDiscountAsync(int id)
    {
        var discount = await _context.DiscountCodes.FindAsync(id);

        if (discount == null)
        {
            return ServiceResult.Fail("Discount not found");
        }

        discount.IsDeleted = true;
        discount.DeletedAt = DateTime.UtcNow;
        discount.IsActive = false;

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAdminEventAsync(
            "DeleteDiscount",
            _currentUserService.UserId ?? 0,
            $"Deleted discount code: {discount.Code}",
            _currentUserService.IpAddress);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IEnumerable<DiscountUsageDto>>> GetDiscountUsagesAsync(int discountId, int page, int pageSize)
    {
        var usages = await _context.DiscountUsages
            .Where(u => u.DiscountCodeId == discountId)
            .Include(u => u.User)
            .OrderByDescending(u => u.UsedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new DiscountUsageDto
            {
                Id = u.Id,
                UserId = u.UserId,
                UserName = u.User.FirstName + " " + u.User.LastName,
                OrderId = u.OrderId,
                DiscountAmount = u.DiscountAmount,
                UsedAt = u.UsedAt,
                IsConfirmed = u.IsConfirmed
            })
            .ToListAsync();

        return ServiceResult<IEnumerable<DiscountUsageDto>>.Ok(usages);
    }
}