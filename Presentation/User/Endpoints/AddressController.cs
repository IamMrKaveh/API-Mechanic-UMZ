using Application.User.Features.Commands.CreateUserAddress;
using Application.User.Features.Commands.DeleteUserAddress;
using Application.User.Features.Commands.UpdateUserAddress;
using Application.User.Features.Queries.GetUserAddresses;
using Application.User.Features.Shared;
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/profile/addresses")]
[Authorize]
public sealed class AddressController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet()]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserAddressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAddresses(CancellationToken ct)
    {
        var query = new GetUserAddressesQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost()]
    [ProducesResponseType(typeof(ApiResponse<UserAddressDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddAddress(
       [FromBody] CreateUserAddressRequest request,
       CancellationToken ct)
    {
        var command = new CreateUserAddressCommand(
            CurrentUser.UserId,
            request.Title,
            request.ReceiverName,
            request.PhoneNumber,
            request.Province,
            request.City,
            request.Address,
            request.PostalCode,
            request.IsDefault,
            request.Latitude,
            request.Longitude);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(
    Guid id,
    [FromBody] UpdateUserAddressRequest request,
    CancellationToken ct)
    {
        var command = new UpdateUserAddressCommand(
            CurrentUser.UserId,
            id,
            request.Title,
            request.ReceiverName,
            request.PhoneNumber,
            request.Province,
            request.City,
            request.Address,
            request.PostalCode,
            request.IsDefault,
            request.Latitude,
            request.Longitude);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var command = new DeleteUserAddressCommand(CurrentUser.UserId, id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}