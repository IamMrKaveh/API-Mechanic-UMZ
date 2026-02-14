namespace Application.Categories.Features.Commands.CreateCategoryGroup;

public class CreateCategoryGroupHandler : IRequestHandler<CreateCategoryGroupCommand, ServiceResult<int>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<CreateCategoryGroupHandler> _logger;

    public CreateCategoryGroupHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<CreateCategoryGroupHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> Handle(
        CreateCategoryGroupCommand request, CancellationToken cancellationToken)
    {
        // بارگذاری Aggregate Root
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.CategoryId, cancellationToken);
        if (category == null)
            return ServiceResult<int>.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            // افزودن گروه از طریق Aggregate - یکتایی نام در Domain بررسی می‌شود
            var group = category.AddGroup(request.Name, request.Description);

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // آپلود آیکون (اختیاری)
            if (request.IconFile != null)
            {
                try
                {
                    await _mediaService.AttachFileToEntityAsync(
                        request.IconFile.Content,
                        request.IconFile.FileName,
                        request.IconFile.ContentType,
                        request.IconFile.Length,
                        "CategoryGroup",
                        group.Id,
                        isPrimary: true);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در آپلود آیکون گروه {GroupId}", group.Id);
                }
            }

            return ServiceResult<int>.Success(group.Id);
        }
        catch (DuplicateCategoryGroupNameException ex)
        {
            return ServiceResult<int>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<int>.Failure(ex.Message);
        }
    }
}