using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Commands.UpdateProductDetails;
using Application.Product.Features.Queries.GetAdminProduct;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Queries.GetAdminProducts;
using Application.Product.Features.Shared;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/v{version:apiVersion}/admin/products")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductsController(
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] GetAdminProductsRequest request)
    {
        var query = new GetAdminProductsQuery(
            request.CategoryId,
            request.BrandId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize
        );

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetAdminProductQuery(id);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse<AdminProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetail(Guid id)
    {
        var query = new GetAdminProductDetailQuery(id);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.CategoryId,
            request.BrandId,
            request.Name,
            request.Description,
            request.Price,
            request.Slug
        );

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(
            id,
            request.CategoryId,
            request.BrandId,
            request.Name,
            request.Price,
            request.Slug,
            request.Description,
            request.IsActive,
            request.IsFeatured,
            request.RowVersion
        );

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("prices/bulk")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesRequest request)
    {
        var command = new BulkUpdatePricesCommand(
            request.Updates
        );

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductDetails(Guid id, [FromBody] UpdateProductDetailsRequest request)
    {
        var command = new UpdateProductDetailsCommand(
            id,
            request.Name,
            request.Description,
            request.BrandId,
            request.IsActive,
            request.Sku,
            request.RowVersion
        );

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var command = new DeleteProductCommand(id);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProduct(Guid id)
    {
        var command = new ActivateProductCommand(id);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateProduct(Guid id)
    {
        var command = new DeactivateProductCommand(id);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreProduct(Guid id)
    {
        var command = new RestoreProductCommand(id);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }
}