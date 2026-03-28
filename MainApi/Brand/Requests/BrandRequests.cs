namespace MainApi.Brand.Requests;

public record CreateBrandRequest(
    int CategoryId,
    string Name,
    string? Description,
    IFormFile? IconFile
);

public record UpdateBrandRequest(
    int CategoryId,
    string Name,
    string? Description,
    IFormFile? IconFile,
    string RowVersion
);

public record MoveBrandRequest(
    int SourceCategoryId,
    int TargetCategoryId,
    int BrandId
);