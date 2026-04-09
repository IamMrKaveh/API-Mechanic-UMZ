using Application.Variant.Features.Commands.AddStock;
using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.RemoveStock;
using Application.Variant.Features.Commands.RemoveVariant;
using Application.Variant.Features.Commands.UpdateVariant;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Endpoints;

[ApiController]
[Route("api/admin/products/{productId}/variants")]
[Authorize(Roles = "Admin")]
public class AdminVariantController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Add(Guid productId, [FromBody] AddVariantRequest request)
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
            request.AttributeValueIds?.Select(a => a).ToList() ?? [],
            request.EnabledShippingMethodIds);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromBody] UpdateVariantRequest request)
    {
        var command = new UpdateVariantCommand(
            request.ProductId,
            request.VariantId,
            request.Sku,
            request.PurchasePrice,
            request.SellingPrice,
            request.OriginalPrice,
            request.Stock,
            request.IsUnlimited,
            request.ShippingMultiplier,
            request.AttributeValueIds,
            request.EnabledShippingMethodIds);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid productId, Guid variantId)
    {
        var command = new RemoveVariantCommand(productId, variantId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/stock")]
    public async Task<IActionResult> AddStock(Guid id, [FromBody] AddStockRequest request)
    {
        var command = new AddStockCommand(
            id,
            request.Quantity,
            CurrentUser.UserId,
            request.Notes);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/remove-stock")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] RemoveStockRequest request)
    {
        var command = new RemoveStockCommand(
            VariantId.From(Guid.Empty),
            request.Quantity,
            UserId.From(CurrentUser.UserId),
            request.Notes);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}