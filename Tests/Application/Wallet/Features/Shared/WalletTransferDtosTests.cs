using Application.Wallet.Features.Shared;

namespace Tests.Application.Wallet.Features.Shared;

public class WalletTransferDtosTests
{
    [Fact]
    public void PreviewDto_Defaults_AreEmpty()
    {
        var dto = new WalletTransferPreviewDto();

        dto.RecipientUserId.ShouldBe(default(Guid));
        dto.RecipientDisplayName.ShouldBeNull();
        dto.Amount.ShouldBe(0m);
        dto.CanProceed.ShouldBeFalse();
        dto.Warning.ShouldBeNull();
    }

    [Fact]
    public void PreviewDto_RoundTrip()
    {
        var dto = new WalletTransferPreviewDto
        {
            RecipientUserId = Guid.NewGuid(),
            RecipientDisplayName = "Sara Ahmadi",
            RecipientPhoneMasked = "0912***6789",
            Amount = 100_000m,
            SenderAvailableBalance = 500_000m,
            DailyLimit = 1_000_000m,
            AlreadyTransferredToday = 200_000m,
            RemainingDailyLimit = 800_000m,
            CanProceed = true,
            Warning = null
        };

        dto.CanProceed.ShouldBeTrue();
        dto.RemainingDailyLimit.ShouldBe(800_000m);
    }

    [Fact]
    public void InitiateResultDto_RoundTrip()
    {
        var dto = new InitiateWalletTransferResultDto
        {
            TransferId = Guid.NewGuid(),
            SenderPhoneMasked = "0912***0000",
            OtpExpiresAt = new DateTime(2026, 1, 1, 12, 5, 0),
            OtpTtlSeconds = 300,
            OtpLength = 6
        };

        dto.OtpTtlSeconds.ShouldBe(300);
        dto.OtpLength.ShouldBe(6);
    }

    [Fact]
    public void ConfirmResultDto_RoundTrip()
    {
        var dto = new ConfirmWalletTransferResultDto
        {
            TransferId = Guid.NewGuid(),
            Status = "Completed",
            Amount = 100_000m,
            RecipientDisplayName = "Sara",
            CorrelationId = "corr-1",
            CompletedAt = new DateTime(2026, 1, 1, 12, 10, 0)
        };

        dto.Status.ShouldBe("Completed");
        dto.CorrelationId.ShouldBe("corr-1");
    }
}
