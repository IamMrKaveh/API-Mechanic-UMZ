using Application.Wallet.Features.Shared;
using Application.Wallet.Mapping;
using Mapster;

namespace Tests.Application.Wallet.Mapping;

public class WalletTransferMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public WalletTransferMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new WalletTransferMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Transfer_ToConfirmResult_MapsIdentityAmountAndCorrelation()
    {
        var transfer = new WalletTransferBuilder().WithAmount(60_000m).Build();

        var dto = _mapper.Map<ConfirmWalletTransferResultDto>(transfer);

        dto.TransferId.ShouldBe(transfer.Id.Value);
        dto.Status.ShouldBe(transfer.Status.ToString());
        dto.Amount.ShouldBe(60_000m);
        dto.CorrelationId.ShouldBe(transfer.CorrelationId);
        dto.RecipientDisplayName.ShouldBeNull();
    }

    [Fact]
    public void Map_PendingTransfer_FallsBackToCurrentTimeForCompletedAt()
    {
        var before = DateTime.UtcNow;
        var transfer = new WalletTransferBuilder().Build();

        var dto = _mapper.Map<ConfirmWalletTransferResultDto>(transfer);

        dto.CompletedAt.ShouldBeGreaterThanOrEqualTo(before);
        dto.CompletedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new WalletTransferMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
