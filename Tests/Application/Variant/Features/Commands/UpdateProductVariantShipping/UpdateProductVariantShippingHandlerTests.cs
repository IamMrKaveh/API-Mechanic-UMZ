using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateProductVariantShippingHandlerTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly UpdateProductVariantShippingHandler _sut;

    public UpdateProductVariantShippingHandlerTests()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _shippingRepository
            .GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());
        _shippingRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<ShippingId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());

        _sut = new UpdateProductVariantShippingHandler(
            _variantRepository,
            _shippingRepository,
            _auditService,
            _currentUserService);
    }

    private static ProductVariant BuildVariant()
    {
        return new ProductVariantBuilder()
            .WithProductId(ProductId.NewId())
            .Build();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var command = new UpdateVariantShippingCommand(
            Guid.NewGuid(),
            1m,
            100m,
            Array.Empty<Guid>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _variantRepository.DidNotReceiveWithAnyArgs().GetVariantWithShippingsAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _variantRepository
            .GetVariantWithShippingsAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var command = new UpdateVariantShippingCommand(
            Guid.NewGuid(),
            1m,
            100m,
            Array.Empty<Guid>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRequestedShippingIdsNotAllInAllList_ReturnsFailure()
    {
        var variant = BuildVariant();

        _variantRepository
            .GetVariantWithShippingsAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _shippingRepository
            .GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());

        var command = new UpdateVariantShippingCommand(
            variant.Id.Value,
            1m,
            100m,
            new[] { Guid.NewGuid() });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _variantRepository.DidNotReceive().Update(Arg.Any<ProductVariant>());
    }

    [Fact]
    public async Task Handle_WithValidCommandAndNoShippingIds_ReturnsSuccessAndPersists()
    {
        var variant = BuildVariant();

        _variantRepository
            .GetVariantWithShippingsAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _shippingRepository
            .GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());
        _shippingRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<ShippingId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());

        var command = new UpdateVariantShippingCommand(
            variant.Id.Value,
            2m,
            150m,
            Array.Empty<Guid>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        _variantRepository.Received(1).Update(variant);
        await _auditService.Received(1).LogInventoryEventAsync(
            Arg.Any<VariantId>(),
            "UpdateVariantShippings",
            Arg.Any<string>(),
            Arg.Any<UserId>());
    }

    [Fact]
    public async Task Handle_WithValidShippingIds_UpdatesVariantAndPersists()
    {
        var variant = BuildVariant();
        var shipping = new ShippingBuilder().Build();

        _variantRepository
            .GetVariantWithShippingsAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _shippingRepository
            .GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new[] { shipping });
        _shippingRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<ShippingId>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { shipping });

        var command = new UpdateVariantShippingCommand(
            variant.Id.Value,
            1.5m,
            200m,
            new[] { shipping.Id.Value });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        variant.Shippings.ShouldContain(vs => vs.ShippingId == shipping.Id);
        _variantRepository.Received(1).Update(variant);
    }
}
