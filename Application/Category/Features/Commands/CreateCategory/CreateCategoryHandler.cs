namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, ServiceResult<int>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<CreateCategoryHandler> _logger;

    public CreateCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<CreateCategoryHandler> logger
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> Handle(
        CreateCategoryCommand request,
        CancellationToken ct
        )
    {
        
        if (await _categoryRepository.ExistsByNameAsync(request.Name.Trim(), ct: ct))
        {
            return ServiceResult<int>.Failure("دسته‌بندی با این نام قبلاً وجود دارد.");
        }

        
        var category = Domain.Category.Category.Create(request.Name, request.Description);

        await _categoryRepository.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        
        if (request.IconFile != null)
        {
            try
            {
                await _mediaService.AttachFileToEntityAsync(
                    request.IconFile.Content,
                    request.IconFile.FileName,
                    request.IconFile.ContentType,
                    request.IconFile.Length,
                    "Category",
                    category.Id,
                    isPrimary: true);

                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در آپلود آیکون دسته‌بندی {CategoryId}", category.Id);
                
            }
        }

        return ServiceResult<int>.Success(category.Id);
    }
}