namespace MainApi.Inventory.Controllers;

/// <summary>
/// FIX #12: Public endpoint برای بررسی real-time موجودی واریانت
/// بدون نیاز به Auth - برای استفاده در frontend پیش از Checkout
/// </summary>
[ApiController]
[Route("api/inventory")]
[AllowAnonymous]
public class InventoryController : BaseApiController
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// بررسی real-time موجودی واریانت - کش‌شده با TTL کوتاه
    /// استفاده در صفحه محصول و پیش از Checkout
    /// </summary>
    [HttpGet("availability/{variantId}")]
    public async Task<IActionResult> GetVariantAvailability(int variantId)
    {
        var query = new GetVariantAvailabilityQuery(variantId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    /// <summary>
    /// بررسی موجودی چندین واریانت به‌صورت یکجا (برای سبد خرید)
    /// </summary>
    [HttpPost("availability/batch")]
    public async Task<IActionResult> GetBatchAvailability(
        [FromBody] BatchAvailabilityRequest request)
    {
        if (request.VariantIds == null || !request.VariantIds.Any())
            return BadRequest("حداقل یک variantId الزامی است.");

        var tasks = request.VariantIds
            .Distinct()
            .Select(id => _mediator.Send(new GetVariantAvailabilityQuery(id)));

        var results = await Task.WhenAll(tasks);

        var availabilities = results
            .Where(r => r.IsSucceed && r.Data != null)
            .Select(r => r.Data!)
            .ToList();

        return Ok(availabilities);
    }
}

public class BatchAvailabilityRequest
{
    public List<int> VariantIds { get; set; } = [];
}