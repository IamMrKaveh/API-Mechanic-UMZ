using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Variant.Features.Commands.RemoveVariant;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantHandlerTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly RemoveVariantHandler _sut;

    public RemoveVariantHandlerTests()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _sut = new RemoveVariantHandler(
            _variantRepository,
            _auditService,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var command = new RemoveVariantCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _variantRepository.DidNotReceive().Update(Arg.Any<ProductVariant>());
        await _auditService.DidNotReceiveWithAnyArgs().LogProductEventAsync(default!, default!, default!, default!);
    }

    [Fact]
    public async Task Handle_WhenVariantExists_MarksRemovedAndPersistsAndLogsAudit()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder()
            .WithId(variantId)
            .WithProductId(productId)
            .Build();

        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new RemoveVariantCommand(productId.Value, variantId.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        variant.IsDeleted.ShouldBeTrue();
        variant.IsActive.ShouldBeFalse();
        _variantRepository.Received(1).Update(variant);
        await _auditService.Received(1).LogProductEventAsync(
            Arg.Is<ProductId>(p => p == productId),
            "RemoveVariant",
            Arg.Any<string>(),
            Arg.Any<UserId>());
    }
}
