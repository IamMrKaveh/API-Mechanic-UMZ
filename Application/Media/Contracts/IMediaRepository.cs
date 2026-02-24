namespace Application.Media.Contracts;

/// <summary>
/// Repository Interface برای Media Aggregate
/// فقط عملیات‌های ضروری برای Persistence - بدون Business Logic
/// </summary>
public interface IMediaRepository
{
    /// <summary>
    /// دریافت رسانه بر اساس شناسه
    /// </summary>
    Task<Domain.Media.Media?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت رسانه‌های یک موجودیت
    /// </summary>
    Task<IReadOnlyList<Domain.Media.Media>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت رسانه اصلی یک موجودیت
    /// </summary>
    Task<Domain.Media.Media?> GetPrimaryByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// شمارش رسانه‌های یک موجودیت
    /// </summary>
    Task<int> CountByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت تمام مسیرهای فایل (برای Cleanup)
    /// </summary>
    Task<IReadOnlySet<string>> GetAllFilePathsAsync(CancellationToken ct = default);

    /// <summary>
    /// افزودن رسانه جدید
    /// </summary>
    Task AddAsync(Domain.Media.Media media, CancellationToken ct = default);

    /// <summary>
    /// به‌روزرسانی رسانه
    /// </summary>
    void Update(Domain.Media.Media media);

    /// <summary>
    /// حذف فیزیکی رسانه از دیتابیس
    /// </summary>
    void Remove(Domain.Media.Media media);
}