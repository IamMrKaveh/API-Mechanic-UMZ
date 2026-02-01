using Application.Common.Interfaces.User;
using Application.DTOs.Order;
using Application.Features.Orders.Commands.CreateOrder;
using Application.Features.Orders.Commands.DeleteOrder;
using Application.Features.Orders.Commands.UpdateOrder;
using Application.Features.Orders.Commands.UpdateOrderStatus;
using Application.Features.Orders.Queries.GetAdminOrderById;
using Application.Features.Orders.Queries.GetAdminOrders;
using Application.Features.Orders.Queries.GetOrderStatistics;
using MainApi.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminOrdersController(ICurrentUserService currentUserService, IMediator mediator)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int? userId,
        [FromQuery] int? statusId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminOrdersQuery(userId, statusId, fromDate, toDate, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var query = new GetAdminOrderByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { message = "Order not found" });
        }
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusByIdDto dto)
    {
        var command = new UpdateOrderStatusCommand(id, dto.OrderStatusId, dto.RowVersion);
        var result = await _mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var command = new UpdateOrderCommand(id, dto);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var command = new DeleteOrderCommand(id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = new GetOrderStatisticsQuery(fromDate, toDate);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(dto, idempotencyKey);
        var result = await _mediator.Send(command);

        // فرض بر این است که نتیجه Command شامل ID سفارش جدید است
        return CreatedAtAction(nameof(GetOrderById), new { id = result.Data.Id }, new { orderId = result.Data.Id });
    }
}