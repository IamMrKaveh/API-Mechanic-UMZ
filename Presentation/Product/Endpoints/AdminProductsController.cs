using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Queries.GetAdminProducts;
using Application.Product.Features.Queries.GetProduct;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/admin/products")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] AdminProductSearchRequest request)
    {
        var query = new GetAdminProductsQuery(
            request.CategoryId,
            request.BrandId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetProductQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Slug,
            request.Description,
            request.Price,
            request.CategoryId,
            request.BrandId,
            CurrentUser.UserId);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Price,
            request.Slug,
            request.Description,
            request.CategoryId,
            request.BrandId,
            request.IsActive,
            request.IsFeatured,
            request.RowVersion,
            CurrentUser.UserId);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var command = new DeleteProductCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}