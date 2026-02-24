namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly IMediaQueryService _mediaQueryService;
    private readonly ILogger<DeleteCategoryHandler> _logger;

    public DeleteCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        IMediaQueryService mediaQueryService,
        ILogger<DeleteCategoryHandler> logger
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _mediaQueryService = mediaQueryService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        DeleteCategoryCommand request,
        CancellationToken ct
        )
    {
        var category = await _categoryRepository.GetByIdWithGroupsAndProductsAsync(request.Id, ct);
        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            category.Delete(request.DeletedBy);
        }
        catch (CategoryHasActiveProductsException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        _categoryRepository.Update(category);

        try
        {
            var mediaList = await _mediaQueryService.GetEntityMediaAsync("Category", request.Id);
            foreach (var media in mediaList)
                await _mediaService.DeleteMediaAsync(media.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف مدیای دسته‌بندی {CategoryId}", request.Id);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}