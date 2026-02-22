namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly IMediaQueryService _mediaQueryService;
    private readonly ILogger<DeleteBrandHandler> _logger;

    public DeleteBrandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        IMediaQueryService mediaQueryService,
        ILogger<DeleteBrandHandler> logger
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _mediaQueryService = mediaQueryService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        DeleteBrandCommand request,
        CancellationToken ct
        )
    {
        var category = await _categoryRepository.GetByIdWithGroupsAndProductsAsync(
            request.CategoryId, ct);

        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            category.RemoveBrand(request.BrandId, request.DeletedBy);

            _categoryRepository.Update(category);

            try
            {
                var mediaList = await _mediaQueryService.GetEntityMediaAsync("CategoryGroup", request.BrandId);
                foreach (var media in mediaList)
                    await _mediaService.DeleteMediaAsync(media.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در حذف مدیای گروه {GroupId}", request.BrandId);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (BrandNotFoundException)
        {
            return ServiceResult.Failure("گروه یافت نشد.", 404);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}