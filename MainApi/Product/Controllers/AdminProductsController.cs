using Application.Common.Results;
using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.ChangePrice;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Commands.UpdateProductDetails;
using Application.Product.Features.Queries.GetAdminProductById;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Queries.GetAdminProducts;
using Application.Product.Features.Shared;
using MainApi.Product.Requests;
using SharedKernel.Models;

namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ServiceResult<PaginatedResult<AdminProductListDto>>>> GetAll(
        [FromQuery] AdminProductSearchParams searchParams)
    {
        var query = new GetAdminProductsQuery(searchParams);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResult<AdminProductViewDto?>>> GetById(int id)
    {
        var query = new GetAdminProductByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var result = await _mediator.Send(new GetAdminProductDetailQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.CategoryId,
            request.BrandId);

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductInput input)
    {
        var command = new UpdateProductCommand(input);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{productId:int}/details")]
    public async Task<IActionResult> UpdateDetails(int productId, [FromBody] UpdateProductDetailsRequest request, CancellationToken ct)
    {
        var command = new UpdateProductDetailsCommand(productId, request.Name, request.Description, request.CategoryId, request.BrandId);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResult>> Delete(int id)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await _mediator.Send(new RestoreProductCommand(id, CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id));
        return ToActionResult(result);
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id));
        return ToActionResult(result);
    }

    [HttpPut("{productId:int}/price")]
    public async Task<IActionResult> ChangePrice(int productId, [FromBody] ChangePriceRequest request, CancellationToken ct)
    {
        var command = new ChangePriceCommand(productId, request.NewPrice);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("prices/bulk")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesRequest request, CancellationToken ct)
    {
        var items = request.Items.Select(i => new PriceUpdateItem(i.ProductId, i.NewPrice)).ToList();
        var command = new BulkUpdatePricesCommand(items);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }
}