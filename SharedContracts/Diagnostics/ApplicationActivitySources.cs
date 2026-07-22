using System.Diagnostics;

namespace SharedContracts.Diagnostics;

public static class ApplicationActivitySources
{
    public const string PaymentName = "Mechanic.Payment";
    public const string OrderName = "Mechanic.Order";
    public const string WalletName = "Mechanic.Wallet";
    public const string SmsName = "Mechanic.Sms";
    public const string OutboxName = "Mechanic.Outbox";
    public const string SearchName = "Mechanic.Search";

    public static readonly ActivitySource Payment = new(PaymentName);
    public static readonly ActivitySource Order = new(OrderName);
    public static readonly ActivitySource Wallet = new(WalletName);
    public static readonly ActivitySource Sms = new(SmsName);
    public static readonly ActivitySource Outbox = new(OutboxName);
    public static readonly ActivitySource Search = new(SearchName);

    public static IReadOnlyList<string> AllNames { get; } =
    [
        PaymentName,
        OrderName,
        WalletName,
        SmsName,
        OutboxName,
        SearchName
    ];
}
