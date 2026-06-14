using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Brand.Requests;

public sealed class CreateBrandRequest
{
    [FromForm(Name = "CategoryId")]
    [Required]
    public Guid CategoryId { get; set; }

    [FromForm(Name = "Name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [FromForm(Name = "Slug")]
    public string? Slug { get; set; }

    [FromForm(Name = "Description")]
    public string? Description { get; set; }

    [FromForm(Name = "LogoFile")]
    public IFormFile? LogoFile { get; set; }
}

public sealed class UpdateBrandRequest
{
    [FromForm(Name = "Name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [FromForm(Name = "CategoryId")]
    [Required]
    public Guid CategoryId { get; set; }

    [FromForm(Name = "Slug")]
    public string? Slug { get; set; }

    [FromForm(Name = "Description")]
    public string? Description { get; set; }

    [FromForm(Name = "LogoFile")]
    public IFormFile? LogoFile { get; set; }

    [FromForm(Name = "RowVersion")]
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public record MoveBrandRequest(
    Guid BrandId,
    Guid TargetCategoryId
);

public record GetAdminBrandsRequest
{
    public Guid? CategoryId { get; init; }
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public bool IncludeDeleted { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public record GetPublicBrandsRequest(Guid? CategoryId);