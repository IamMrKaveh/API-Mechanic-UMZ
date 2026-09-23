using Application.Payment.Features.Shared;
using Application.Payment.Mapping;
using Mapster;

namespace Tests.Application.Payment.Mapping;

public class PaymentMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public PaymentMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new PaymentMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_PaymentTransaction_ToDto_MapsAllFields()
    {
        var transaction = new PaymentTransactionBuilder()
            .WithAuthority("AUTH-123")
            .WithGateway("Zarinpal")
            .WithAmount(750_000m)
            .Build();

        var dto = _mapper.Map<PaymentTransactionDto>(transaction);

        dto.Id.ShouldBe(transaction.Id.Value);
        dto.OrderId.ShouldBe(transaction.OrderId.Value);
        dto.UserId.ShouldBe(transaction.UserId.Value);
        dto.Authority.ShouldBe("AUTH-123");
        dto.Gateway.ShouldBe("Zarinpal");
        dto.Amount.ShouldBe(750_000m);
        dto.Status.ShouldBe(transaction.Status.Value);
        dto.StatusDisplayName.ShouldBe(transaction.Status.DisplayName);
        dto.RefId.ShouldBe(transaction.RefId);
        dto.IsSuccessful.ShouldBe(transaction.IsSuccessful());
        dto.VerifiedAt.ShouldBe(transaction.VerifiedAt);
        dto.ExpiresAt.ShouldBe(transaction.ExpiresAt);
        dto.CreatedAt.ShouldBe(transaction.CreatedAt);
        dto.UpdatedAt.ShouldBe(transaction.UpdatedAt);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new PaymentMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
