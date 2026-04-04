using Application.Common.Results;
using Application.Media.Contracts;
using Domain.Category.Exceptions;
using Domain.Category.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMediaService mediaService,
    IMediaQueryService mediaQueryService,
    ILogger<DeleteCategoryHandler> logger) : IRequestHandler<DeleteCategoryCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMediaService _mediaService = mediaService;
    private readonly IMediaQueryService _mediaQueryService = mediaQueryService;
    private readonly ILogger<DeleteCategoryHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        DeleteCategoryCommand request,
        CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdWithGroupsAndProductsAsync(request.Id, ct);
        if (category == null)
            return ServiceResult.NotFound("دسته‌بندی یافت نشد.");

        try
        {
            category.Delete(request.DeletedBy);
        }
        catch (CategoryHasActiveProductsException ex)
        {
            return ServiceResult.Forbidden(ex.Message);
        }

        _categoryRepository.Update(category);

        try
        {
            var mediaList = await _mediaQueryService.GetEntityMediaAsync("Category", request.Id, ct);
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