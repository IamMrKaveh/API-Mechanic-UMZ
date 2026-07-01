using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Seeders;

public sealed class PaymentMethodSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentMethodSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();

        try
        {
            await EnsureMethodAsync(
                context,
                PaymentMethodCode.ZarinpalSandbox,
                "درگاه پرداخت تستی زرین‌پال",
                "درگاه آزمایشی زرین‌پال برای محیط توسعه و تست",
                feeAmount: 0m,
                feePercentage: 0m,
                sortOrder: 10,
                cancellationToken);

            await EnsureMethodAsync(
                context,
                PaymentMethodCode.Zarinpal,
                "درگاه پرداخت زرین‌پال",
                "پرداخت آنلاین از طریق درگاه زرین‌پال",
                feeAmount: 0m,
                feePercentage: 0m,
                sortOrder: 20,
                cancellationToken);

            await EnsureMethodAsync(
                context,
                PaymentMethodCode.CashOnDelivery,
                "پرداخت در محل",
                "تسویه وجه به‌صورت نقدی هنگام تحویل سفارش",
                feeAmount: 0m,
                feePercentage: 0m,
                sortOrder: 30,
                cancellationToken);

            await EnsureMethodAsync(
                context,
                PaymentMethodCode.Wallet,
                "کیف پول",
                "پرداخت با موجودی کیف پول کاربر",
                feeAmount: 0m,
                feePercentage: 0m,
                sortOrder: 40,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentMethod seeding failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureMethodAsync(
        DBContext context,
        string code,
        string name,
        string description,
        decimal feeAmount,
        decimal feePercentage,
        int sortOrder,
        CancellationToken ct)
    {
        var methodCode = PaymentMethodCode.Create(code);
        var exists = await context.PaymentMethods
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Code == methodCode, ct);

        if (exists) return;

        var method = PaymentMethod.Create(
            PaymentMethodName.Create(name),
            methodCode,
            PaymentMethodFee.Create(feeAmount, feePercentage),
            description,
            iconUrl: null,
            sortOrder: sortOrder);

        await context.PaymentMethods.AddAsync(method, ct);
    }
}