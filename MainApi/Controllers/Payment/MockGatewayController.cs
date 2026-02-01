namespace MainApi.Controllers.Payment;

[ApiController]
[Route("api/mock-gateway")]
public class MockGatewayController : ControllerBase
{
    [HttpGet("pay")]
    public ContentResult Pay([FromQuery] decimal amount, [FromQuery] string authority, [FromQuery] string callback)
    {
        var html = $@"
        <!DOCTYPE html>
        <html lang='fa' dir='rtl'>
        <head>
            <title>درگاه پرداخت تستی</title>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: system-ui, -apple-system, sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; background: #f3f4f6; margin: 0; }}
                .card {{ background: white; padding: 2.5rem; border-radius: 1.5rem; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1); text-align: center; max-width: 400px; width: 90%; }}
                .btn {{ display: block; width: 100%; padding: 1rem; border: none; border-radius: 0.75rem; font-size: 1rem; font-weight: bold; cursor: pointer; margin-top: 1rem; color: white; text-decoration: none; transition: opacity 0.2s; }}
                .btn:hover {{ opacity: 0.9; }}
                .btn-success {{ background: #10b981; box-shadow: 0 4px 6px -1px rgba(16, 185, 129, 0.3); }}
                .btn-danger {{ background: #ef4444; box-shadow: 0 4px 6px -1px rgba(239, 68, 68, 0.3); }}
                .amount {{ font-size: 2rem; font-weight: 900; color: #111827; margin: 1.5rem 0; }}
                .label {{ color: #6b7280; font-size: 0.875rem; margin-bottom: 0.5rem; }}
                .auth-code {{ background: #f3f4f6; padding: 0.5rem; border-radius: 0.5rem; font-family: monospace; color: #4b5563; font-size: 0.875rem; margin-bottom: 2rem; }}
            </style>
        </head>
        <body>
            <div class='card'>
                <h2 style='margin-top:0; color:#374151;'>درگاه پرداخت تستی</h2>
                
                <div>
                    <div class='label'>مبلغ قابل پرداخت</div>
                    <div class='amount'>{amount:N0} <span style='font-size: 1rem; font-weight: normal;'>تومان</span></div>
                </div>

                <div class='label'>کد پیگیری</div>
                <div class='auth-code'>{authority}</div>
                
                <a href='{callback}?authority={authority}&status=OK' class='btn btn-success'>پرداخت موفق</a>
                <a href='{callback}?authority={authority}&status=NOK' class='btn btn-danger'>انصراف / پرداخت ناموفق</a>
            </div>
        </body>
        </html>";

        return base.Content(html, "text/html");
    }
}