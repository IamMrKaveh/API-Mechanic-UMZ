namespace Infrastructure.Common.Options;

public sealed class FrontendUrlsOptions
{
    public const string SectionName = "FrontendUrls";

    public string BaseUrl { get; set; } = "http://localhost:4200";

    public string LocalHostUrl { get; set; } = "http://localhost:4200";

    public string PaymentSuccessPath { get; set; } = "/payment/success";

    public string PaymentFailurePath { get; set; } = "/payment/failure";

    public string WalletTopUpCallbackPath { get; set; } = "/dashboard/wallet/topup/callback";
}