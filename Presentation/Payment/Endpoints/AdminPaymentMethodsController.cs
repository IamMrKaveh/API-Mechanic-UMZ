using Application.Payment.Features.Commands.ActivatePaymentMethod;
using Application.Payment.Features.Commands.CreatePaymentMethod;
using Application.Payment.Features.Commands.DeactivatePaymentMethod;
using Application.Payment.Features.Commands.DeletePaymentMethod;
using Application.Payment.Features.Commands.UpdatePaymentMethod;
using Application.Payment.Features.Queries.GetPaymentMethod;
using Application.Payment.Features.Queries.GetPaymentMethods;
using Application.Payment.Features.Shared;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/payment-methods")]
[Authorize(Roles = "Admin")]
public sealed class AdminPaymentMethodsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PaymentMethodListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods(
        [FromQuery] bool includeInactive = true,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        return await Send(new GetPaymentMethodsQuery(includeInactive, includeDeleted), ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentMethodById(Guid id, CancellationToken ct)
    {
        return await Send(new GetPaymentMethodQuery(id), ct);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePaymentMethod(
        [FromBody] CreatePaymentMethodRequest request,
        CancellationToken ct)
    {
        return await Send(Mapper.Map<CreatePaymentMethodCommand>(request), ct);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePaymentMethod(
        Guid id,
        [FromBody] UpdatePaymentMethodRequest request,
        CancellationToken ct)
    {
        return await Send(Mapper.Map<UpdatePaymentMethodCommand>(request) with { Id = id }, ct);
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivatePaymentMethod(Guid id, CancellationToken ct)
    {
        return await Send(new ActivatePaymentMethodCommand(id), ct);
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePaymentMethod(Guid id, CancellationToken ct)
    {
        return await Send(new DeactivatePaymentMethodCommand(id), ct);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePaymentMethod(Guid id, CancellationToken ct)
    {
        return await Send(new DeletePaymentMethodCommand(id), ct);
    }
}