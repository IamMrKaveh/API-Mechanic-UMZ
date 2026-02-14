namespace MainApi.Order.Controllers;

[Route("api/admin/order-statuses")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrderStatusController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminOrderStatusController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderStatuses()
    {
        var query = new GetOrderStatusesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderStatus([FromBody] CreateOrderStatusCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetOrderStatus), new { id = result.Data!.Id }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        // نیاز به Query اختصاصی GetById یا استفاده از متدهای Repository در یک Query جدید
        // فعلا از GetOrderStatuses و فیلتر استفاده می‌کنیم یا باید Query جدید بسازیم
        // با فرض وجود GetOrderStatusByIdQuery:
        // var result = await _mediator.Send(new GetOrderStatusByIdQuery(id));
        // return ToActionResult(result);
        return StatusCode(501, "Implement GetOrderStatusByIdQuery");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        // این هندلر باید جدا از UpdateOrderStatusCommand مربوط به سفارش باشد
        // UpdateOrderStatusDefinitionCommand لازم است
        return StatusCode(501, "Implement UpdateOrderStatusDefinitionCommand");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderStatus(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new DeleteOrderStatusCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}