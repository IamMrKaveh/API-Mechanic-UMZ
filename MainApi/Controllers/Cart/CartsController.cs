using Application.Common.Interfaces.User;
using Application.DTOs.Cart;
using Application.Features.Cart.Commands.AddItemToCart;
using Application.Features.Cart.Commands.ClearCart;
using Application.Features.Cart.Commands.RemoveItemFromCart;
using Application.Features.Cart.Commands.UpdateCartItem;
using Application.Features.Cart.Queries.GetCart;
using Application.Features.Cart.Queries.GetCartItemsCount;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
public class CartsController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public CartsController(
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    private void EnsureGuestId()
    {
        if (!_currentUserService.UserId.HasValue && string.IsNullOrEmpty(_currentUserService.GuestId))
        {
            var newGuestId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Guest-Token", newGuestId);
            // نکته: در سناریوی واقعی کلاینت باید این ه��ر را ذخیره و ارسال کند
            // اینجا فقط برای اینکه در Command استفاده شود، ممکن است نیاز به پاس دادن مقدار باشد
        }
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            guestId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Guest-Token", guestId);
        }

        var query = new GetCartQuery(userId, guestId);
        var result = await _mediator.Send(query);

        if (result.Data == null)
        {
            // سبد خالی برمی‌گرداند به جای نال
            return Ok(new CartDto(0, userId, guestId, new List<CartItemDto>(), 0, 0, new List<CartPriceChangeDto>()));
        }

        return Ok(result.Data);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItemToCart([FromBody] AddToCartDto dto)
    {
        // FluentValidation در Pipeline اجرا می‌شود، بنابراین ModelState.IsValid شاید دیگر نیاز نباشد
        // اما نگه داشتن آن ضرری ندارد.

        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            guestId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Guest-Token", guestId);
        }

        var command = new AddItemToCartCommand(userId, guestId, dto);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { message = result.Error }),
                409 => Conflict(new { message = result.Error }), // OutOfStock or Concurrency
                _ => StatusCode(500, new { message = result.Error })
            };
        }

        return Ok(result.Data);
    }

    [HttpPut("items/{itemId}")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

        var command = new UpdateCartItemCommand(userId, guestId, itemId, dto);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { message = result.Error }),
                409 => Conflict(new { message = result.Error }),
                _ => StatusCode(500, new { message = result.Error })
            };
        }

        return Ok(result.Data);
    }

    [HttpDelete("items/{itemId}")]
    public async Task<ActionResult<CartDto>> RemoveItemFromCart(int itemId)
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

        var command = new RemoveItemFromCartCommand(userId, guestId, itemId);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

        var command = new ClearCartCommand(userId, guestId);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return StatusCode(500, "An error occurred while clearing the cart.");
        }
        return NoContent();
    }

    [HttpGet("items/count")]
    public async Task<ActionResult<int>> GetCartItemsCount()
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Ok(0);

        var query = new GetCartItemsCountQuery(userId, guestId);
        var count = await _mediator.Send(query);
        return Ok(count);
    }
}