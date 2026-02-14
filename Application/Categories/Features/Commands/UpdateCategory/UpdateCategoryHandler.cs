namespace Application.Categories.Features.Commands.UpdateCategory;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<UpdateCategoryHandler> _logger;

    public UpdateCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<UpdateCategoryHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.Id, cancellationToken);
        if (category == null)
            return ServiceResult.Failure("دسته‌بن��ی یافت نشد.", 404);

        // تنظیم RowVersion برای Concurrency Control
        _categoryRepository.SetOriginalRowVersion(category, Convert.FromBase64String(request.RowVersion));

        // بررسی یکتایی نام
        if (await _categoryRepository.ExistsByNameAsync(request.Name.Trim(), request.Id, cancellationToken))
        {
            return ServiceResult.Failure("دسته‌بندی با این نام قبلاً وجود دارد.");
        }

        // بروزرسانی از طریق Aggregate (اعتبارسنجی‌ها در Domain)
        category.Update(request.Name, request.Description, request.SortOrder);

        // مدیریت آیکون
        Domain.Media.Media? newMedia = null;
        if (request.IconFile != null)
        {
            var existingMedia = (await _mediaService.GetEntityMediaAsync("Category", request.Id))
                .FirstOrDefault(m => m.IsPrimary);

            if (existingMedia != null)
                await _mediaService.DeleteMediaAsync(existingMedia.Id);

            newMedia = await _mediaService.AttachFileToEntityAsync(
                request.IconFile.Content,
                request.IconFile.FileName,
                request.IconFile.ContentType,
                request.IconFile.Length,
                "Category",
                request.Id,
                isPrimary: true);
        }

        _categoryRepository.Update(category);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Rollback media upload if concurrency conflict
            if (newMedia != null)
                await _mediaService.DeleteMediaAsync(newMedia.Id);

            return ServiceResult.Failure("این رکورد توسط کاربر دیگری تغییر یافته است. لطفاً مجدداً تلاش کنید.");
        }
    }
}