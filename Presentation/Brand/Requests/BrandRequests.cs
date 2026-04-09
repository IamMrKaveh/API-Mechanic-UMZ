namespace Presentation.Brand.Requests;

public record CreateBrandRequest(
    Guid CategoryId,
    string Name,
    string? Slug,
    string? Description,
    IFormFile? IconFile
);

public record UpdateBrandRequest(
    string Name,
    Guid CategoryId,
    string? Slug,
    string? Description,
    IFormFile? IconFile,
    string RowVersion
);

public record MoveBrandRequest(
    Guid BrandId,
    Guid TargetCategoryId
);