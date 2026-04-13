using Application.Inventory.Features.Commands.AddStock;
using Application.Inventory.Features.Commands.RemoveStock;
using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.RemoveVariant;
using Application.Variant.Features.Commands.UpdateVariant;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Endpoints;

[ApiController]
[Route("api/admin/products/{productId:guid}/variants")]
[Authorize(Roles = "Admin")]
public sealed class AdminVariantController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Add(
        Guid productId,
        [FromBody] AddVariantRequest request,
        CancellationToken ct)
    {
        var command = new AddVariantCommand(
            productId,
            request.Sku,
            request.PurchasePrice,
            request.SellingPrice,
            request.OriginalPrice,
            request.Stock,
            request.IsUnlimited,
            request.ShippingMultiplier,
            request.AttributeValueIds?.ToList() ?? [],
            request.EnabledShippingIds);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{variantId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        Guid variantId,
        [FromBody] UpdateVariantRequest request,
        CancellationToken ct)
    {
        var command = new UpdateVariantCommand(
            productId,
            variantId,
            CurrentUser.UserId,
            request.Sku,
            request.PurchasePrice,
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
    public async Task<IActionResult> Delete(
        Guid productId,
        Guid variantId,
        CancellationToken ct)
    {
        var command = new RemoveVariantCommand(productId, variantId, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{variantId:guid}/stock")]
    public async Task<IActionResult> AddStock(
        Guid variantId,
        [FromBody] AddStockRequest request,
        CancellationToken ct)
    {
        var command = new AddStockCommand(
            variantId,
            request.Quantity,
            CurrentUser.UserId,
            request.Notes);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{variantId:guid}/remove-stock")]
    public async Task<IActionResult> RemoveStock(
        Guid variantId,
        [FromBody] RemoveStockRequest request,
        CancellationToken ct)
    {
        var command = new RemoveStockCommand(
            variantId,
            request.Quantity,
            CurrentUser.UserId,
            request.Notes);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}