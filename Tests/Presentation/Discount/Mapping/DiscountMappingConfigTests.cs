using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Domain.Discount.Enums;
using Mapster;
using Presentation.Discount.Mapping;
using Presentation.Discount.Requests;

namespace Tests.Presentation.Discount.Mapping;

public class DiscountMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly DiscountMappingConfig _sut = new();

    public DiscountMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void CreateDiscountRequest_WithPercentageType_MapsToCommand()
    {
        // Arrange
        var startsAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var expiresAt = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new CreateDiscountRequest("SAVE10", "Percentage", 10, 50000, 100, startsAt, expiresAt);

        // Act
        var command = request.Adapt<CreateDiscountCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Code.ShouldBe(request.Code);
        command.DiscountType.ShouldBe(DiscountType.Percentage);
        command.Value.ShouldBe(request.DiscountValue);
        command.MaximumDiscountAmount.ShouldBe(request.MaximumDiscountAmount);
        command.UsageLimit.ShouldBe(request.UsageLimit);
        command.StartsAt.ShouldBe(request.StartsAt);
        command.ExpiresAt.ShouldBe(request.ExpiresAt);
    }

    [Fact]
    public void CreateDiscountRequest_WithFixedAmountType_MapsToCommand()
    {
        // Arrange
        var request = new CreateDiscountRequest("FIXED5000", "FixedAmount", 5000, null, null, null, null);

        // Act
        var command = request.Adapt<CreateDiscountCommand>(_config);

        // Assert
        command.DiscountType.ShouldBe(DiscountType.FixedAmount);
        command.Value.ShouldBe(5000);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UnknownType")]
    public void CreateDiscountRequest_WithInvalidType_DefaultsToPercentage(string? discountType)
    {
        // Arrange
        var request = new CreateDiscountRequest("SAVE10", discountType, 10, null, null, null, null);

        // Act
        var command = request.Adapt<CreateDiscountCommand>(_config);

        // Assert
        command.DiscountType.ShouldBe(DiscountType.Percentage);
    }

    [Fact]
    public void UpdateDiscountRequest_MapsToCommand_ExceptId()
    {
        // Arrange
        var startsAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var expiresAt = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new UpdateDiscountRequest("FixedAmount", 5000, 20000, 50, startsAt, expiresAt, false);

        // Act
        var command = request.Adapt<UpdateDiscountCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Id.ShouldBe(Guid.Empty);
        command.DiscountType.ShouldBe(DiscountType.FixedAmount);
        command.Value.ShouldBe(request.DiscountValue);
        command.MaximumDiscountAmount.ShouldBe(request.MaximumDiscountAmount);
        command.UsageLimit.ShouldBe(request.UsageLimit);
        command.StartsAt.ShouldBe(request.StartsAt);
        command.ExpiresAt.ShouldBe(request.ExpiresAt);
        command.IsActive.ShouldBe(request.IsActive);
    }

    [Fact]
    public void DiscountMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
