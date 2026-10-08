using SharedContracts.Diagnostics;
using System.Diagnostics;

namespace Tests.SharedContracts.Diagnostics;

public class ApplicationActivitySourcesTests
{
    [Fact]
    public void SourceNames_HaveExpectedValues()
    {
        ApplicationActivitySources.PaymentName.ShouldBe("Mechanic.Payment");
        ApplicationActivitySources.OrderName.ShouldBe("Mechanic.Order");
        ApplicationActivitySources.WalletName.ShouldBe("Mechanic.Wallet");
        ApplicationActivitySources.SmsName.ShouldBe("Mechanic.Sms");
        ApplicationActivitySources.OutboxName.ShouldBe("Mechanic.Outbox");
        ApplicationActivitySources.SearchName.ShouldBe("Mechanic.Search");
    }

    [Fact]
    public void AllNames_ContainsAllSixSources()
    {
        var names = ApplicationActivitySources.AllNames;

        names.Count.ShouldBe(6);
        names.ShouldContain(ApplicationActivitySources.PaymentName);
        names.ShouldContain(ApplicationActivitySources.OrderName);
        names.ShouldContain(ApplicationActivitySources.WalletName);
        names.ShouldContain(ApplicationActivitySources.SmsName);
        names.ShouldContain(ApplicationActivitySources.OutboxName);
        names.ShouldContain(ApplicationActivitySources.SearchName);
    }

    [Fact]
    public void ActivitySources_AreInitialized_WithMatchingNames()
    {
        ApplicationActivitySources.Payment.ShouldNotBeNull();
        ApplicationActivitySources.Payment.Name.ShouldBe(ApplicationActivitySources.PaymentName);
        ApplicationActivitySources.Order.Name.ShouldBe(ApplicationActivitySources.OrderName);
        ApplicationActivitySources.Wallet.Name.ShouldBe(ApplicationActivitySources.WalletName);
        ApplicationActivitySources.Sms.Name.ShouldBe(ApplicationActivitySources.SmsName);
        ApplicationActivitySources.Outbox.Name.ShouldBe(ApplicationActivitySources.OutboxName);
        ApplicationActivitySources.Search.Name.ShouldBe(ApplicationActivitySources.SearchName);
    }

    [Fact]
    public void ActivitySource_CanStartAndStopActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ApplicationActivitySources.Order.StartActivity("test-operation");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("test-operation");
    }
}
