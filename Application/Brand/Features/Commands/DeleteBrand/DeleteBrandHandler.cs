namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<DeleteBrandHandler> _logger;

    public DeleteBrandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<DeleteBrandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        // بارگذاری Aggregate با محصولات (برای بررسی امکان حذف)
        var category = await _categoryRepository.GetByIdWithGroupsAndProductsAsync(
            request.CategoryId, cancellationToken);

        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            // حذف از طریق Aggregate - بررسی محصولات فعال در Domain
            category.RemoveBrand(request.GroupId, request.DeletedBy);

            _categoryRepository.Update(category);

            // حذف مدیا
            try
            {
                var mediaList = await _mediaService.GetEntityMediaAsync("CategoryGroup", request.GroupId);
                foreach (var media in mediaList)
                    await _mediaService.DeleteMediaAsync(media.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در حذف مدیای گروه {GroupId}", request.GroupId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
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