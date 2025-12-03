namespace Application.Common.Interfaces.Admin;

public interface IAdminDiscountService
{
    Task<ServiceResult<PagedResultDto<DiscountCodeDto>>> GetDiscountsAsync(bool includeExpired, int page, int pageSize);
    Task<ServiceResult<DiscountCodeDetailDto?>> GetDiscountByIdAsync(int id);
    Task<ServiceResult<DiscountCodeDto>> CreateDiscountAsync(CreateDiscountDto dto);
    Task<ServiceResult> UpdateDiscountAsync(int id, UpdateDiscountDto dto);
    Task<ServiceResult> DeleteDiscountAsync(int id);
    Task<ServiceResult<IEnumerable<DiscountUsageDto>>> GetDiscountUsagesAsync(int discountId, int page, int pageSize);
}