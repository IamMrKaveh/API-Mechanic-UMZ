using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Commands.UpdateProductDetails;
using Application.Product.Features.Queries.GetAdminProductById;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Queries.GetAdminProducts;
using MapsterMapper;
using Presentation.Product.Requests;
using SharedKernel.Models;

namespace Presentation.Product.Endpoints;

[Route("api/admin/products")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] GetAdminProductsRequest request)
    {
        var query = new GetAdminProductsQuery(
            request.CategoryId,
            request.BrandId,
            CurrentUser.UserId,
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
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetAdminProductByIdQuery(id, CurrentUser.UserId);

        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("detail/{id:guid}")]
    public async Task<IActionResult> GetProductDetail(Guid id)
    {
        var query = new GetAdminProductDetailQuery(id, CurrentUser.UserId);

        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.CategoryId,
            request.BrandId,
            CurrentUser.UserId,
            request.Name,
            request.Description,
            request.Price,
            request.Slug
        );

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
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
            request.RowVersion,
            CurrentUser.UserId
        );

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("bulkupdateprices")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesRequest request)
    {
        var command = new BulkUpdatePricesCommand(
            request.Updates,
            CurrentUser.UserId
        );

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("details/{id:guid}")]
    public async Task<IActionResult> UpdateProductDetails(Guid id, [FromBody] UpdateProductDetailsRequest request)
    {
        var command = new UpdateProductDetailsCommand(
            id,
            request.Name,
            request.Description,
            request.BrandId,
            request.IsActive,
            request.Sku,
            request.RowVersion,
            CurrentUser.UserId
        );

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var command = new DeleteProductCommand(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> ActivateProduct(Guid id)
    {
        var command = new ActivateProductCommand(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(Guid id)
    {
        var command = new DeactivateProductCommand(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> RestoreProduct(Guid id)
    {
        var command = new RestoreProductCommand(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}