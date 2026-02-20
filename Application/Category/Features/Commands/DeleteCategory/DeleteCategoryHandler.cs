using Application.Category.Contracts;
using Domain.Category.Exceptions;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<DeleteCategoryHandler> _logger;

    public DeleteCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<DeleteCategoryHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdWithGroupsAndProductsAsync(request.Id, cancellationToken);
        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            // حذف از طریق Aggregate - بررسی محصولات فعال در Domain انجام می‌شود
            category.Delete(request.DeletedBy);
        }
        catch (CategoryHasActiveProductsException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        _categoryRepository.Update(category);

        // حذف مدیا
        try
        {
            var mediaList = await _mediaService.GetEntityMediaAsync("Category", request.Id);
            foreach (var media in mediaList)
                await _mediaService.DeleteMediaAsync(media.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف مدیای دسته‌بندی {CategoryId}", request.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}