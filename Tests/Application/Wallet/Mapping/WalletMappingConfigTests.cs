using Application.Wallet.Features.Shared;
using Application.Wallet.Mapping;
using Mapster;

namespace Tests.Application.Wallet.Mapping;

public class WalletMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public WalletMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new WalletMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Wallet_ToWalletDto_MapsBalances()
    {
        var wallet = new WalletBuilder().Build();

        var dto = _mapper.Map<WalletDto>(wallet);

        dto.Id.ShouldBe(wallet.Id.Value);
        dto.UserId.ShouldBe(wallet.OwnerId.Value);
        dto.CurrentBalance.ShouldBe(wallet.Balance.Amount);
        dto.ReservedBalance.ShouldBe(wallet.ReservedBalance.Amount);
        dto.AvailableBalance.ShouldBe(wallet.AvailableBalance.Amount);
        dto.CreatedAt.ShouldBe(wallet.CreatedAt);
        dto.UpdatedAt.ShouldBe(wallet.UpdatedAt);
    }

    [Fact]
    public void Map_LedgerEntry_ToDto_MapsTransactionAndFlagsPlainDescription()
    {
        var entry = new WalletLedgerEntryBuilder()
            .WithAmount(25_000m)
            .WithBalanceAfter(125_000m)
            .WithDescription("Topup via gateway")
            .Build();

        var dto = _mapper.Map<WalletLedgerEntryDto>(entry);

        dto.Id.ShouldBe(entry.Id.Value);
        dto.WalletId.ShouldBe(entry.WalletId.Value);
        dto.UserId.ShouldBe(entry.OwnerId.Value);
        dto.AmountDelta.ShouldBe(25_000m);
        dto.BalanceAfter.ShouldBe(125_000m);
        dto.TransactionType.ShouldBe(entry.TransactionType.ToString());
        dto.Description.ShouldBe("Topup via gateway");
        dto.ReferenceId.ShouldBe(Guid.Parse(entry.ReferenceId));
        dto.CreatedAt.ShouldBe(entry.OccurredAt);
        dto.IsAdminAdjustment.ShouldBeFalse();
    }

    [Fact]
    public void Map_LedgerEntry_WithAdminPrefix_MarksAdminAdjustment()
    {
        var entry = new WalletLedgerEntryBuilder()
            .WithDescription("[ADMIN-42] manual correction")
            .Build();

        var dto = _mapper.Map<WalletLedgerEntryDto>(entry);

        dto.IsAdminAdjustment.ShouldBeTrue();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new WalletMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
