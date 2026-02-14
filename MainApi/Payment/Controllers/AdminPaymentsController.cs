namespace MainApi.Payment.Controllers;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentSearchParams searchParams)
    {
        var result = await _mediator.Send(new GetAdminPaymentsQuery(searchParams));
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }
}