using Application.Inventory.Features.Commands.AddStock;
using Application.Inventory.Features.Commands.RemoveStock;
using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.RemoveVariant;
using Application.Variant.Features.Commands.UpdateVariant;
using Application.Variant.Features.Shared;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/products/variants")]
[Authorize(Roles = "Admin")]
public sealed class AdminVariantController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductVariantViewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(
        [FromQuery] Guid productId,
        [FromBody] AddVariantRequest request,
        CancellationToken ct)
    {
        if (productId == Guid.Empty)
            return BadRequest(new ApiResponse(false, "شناسه محصول الزامی است."));

        if (request is null)
            return BadRequest(new ApiResponse(false, "بدنه درخواست الزامی است."));

        var command = new AddVariantCommand(
            productId,
            request.Sku,
            request.SellingPrice,
            request.OriginalPrice,
            request.Stock,
            request.IsUnlimited,
            request.ShippingMultiplier,
            request.AttributeValueIds?.ToList() ?? new List<Guid>(),
            request.EnabledShippingIds?.ToList());

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("{variantId:guid}/stock")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddStock(
        Guid variantId,
        [FromBody] AddStockRequest request,
        CancellationToken ct)
    {
        var command = new AddStockCommand(variantId, request.Quantity, request.Notes);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromQuery] Guid productId,
        Guid variantId,
        [FromBody] UpdateVariantRequest request,
        CancellationToken ct)
    {
        var command = new UpdateVariantCommand(
            productId,
            variantId,
            request.Sku,
            request.SellingPrice,
            request.OriginalPrice,
            request.Stock,
            request.IsUnlimited,
            request.ShippingMultiplier,
            request.AttributeValueIds,
            request.EnabledShippingIds);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromQuery] Guid productId,
        Guid variantId,
        CancellationToken ct)
    {
        var command = new RemoveVariantCommand(productId, variantId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{variantId:guid}/stock")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStock(
        Guid variantId,
        [FromBody] RemoveStockRequest request,
        CancellationToken ct)
    {
        var command = new RemoveStockCommand(variantId, request.Quantity, request.Notes);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}