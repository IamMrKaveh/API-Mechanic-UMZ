namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly IMediaQueryService _mediaQueryService;
    private readonly ILogger<UpdateCategoryHandler> _logger;

    public UpdateCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        IMediaQueryService mediaQueryService,
        ILogger<UpdateCategoryHandler> logger
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _mediaQueryService = mediaQueryService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        UpdateCategoryCommand request,
        CancellationToken ct
        )
    {
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.Id, ct);
        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        _categoryRepository.SetOriginalRowVersion(category, Convert.FromBase64String(request.RowVersion));

        if (await _categoryRepository.ExistsByNameAsync(request.Name.Trim(), request.Id, ct))
        {
            return ServiceResult.Failure("دسته‌بندی با این نام قبلاً وجود دارد.");
        }

        category.Update(request.Name, request.Description, request.SortOrder);

        Domain.Media.Media? newMedia = null;
        if (request.IconFile != null)
        {
            var existingMedia = (await _mediaQueryService.GetEntityMediaAsync("Category", request.Id))
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
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            if (newMedia != null)
                await _mediaService.DeleteMediaAsync(newMedia.Id);

            return ServiceResult.Failure("این رکورد توسط کاربر دیگری تغییر یافته است. لطفاً مجدداً تلاش کنید.");
        }
    }
}