using Application.Brand.Features.Commands.DeleteBrand;
using Domain.Brand.Exceptions;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Brands = Domain.Brand.Aggregates.Brand;

namespace Tests.Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandlerTests
{
    private readonly IBrandRepository _brandRepository = Substitute.For<IBrandRepository>(); private readonly DeleteBrandHandler _sut;

    public DeleteBrandHandlerTests()
    {
        _sut = new DeleteBrandHandler(_brandRepository);
    }

    [Fact]
    public async Task Handle_WhenBrandNotFound_ReturnsNotFound()
    {
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((Brands?)null);

        var result = await _sut.Handle(
            new DeleteBrandCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _brandRepository.DidNotReceive().Update(Arg.Any<Brands>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenBrandFound_DeactivatesBrandAndReturnsSuccess()
    {
        var brand = await new BrandBuilder().BuildAsync();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);

        var result = await _sut.Handle(
            new DeleteBrandCommand(brand.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        brand.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenBrandFound_CallsUpdateOnceOnRepository()
    {
        var brand = await new BrandBuilder().BuildAsync();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);

        await _sut.Handle(new DeleteBrandCommand(brand.Id.Value), CancellationToken.None);

        _brandRepository.Received(1).Update(brand, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenBrandAlreadyDeactivated_ThrowsBrandAlreadyDeactivatedException()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.Deactivate();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);

        await Should.ThrowAsync<BrandAlreadyDeactivatedException>(
            () => _sut.Handle(new DeleteBrandCommand(brand.Id.Value), CancellationToken.None));

        _brandRepository.DidNotReceive().Update(Arg.Any<Brands>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_PassesBrandIdBuiltFromRequestIdToRepository()
    {
        var id = Guid.NewGuid();
        BrandId? captured = null;
        _brandRepository
            .GetByIdAsync(Arg.Do<BrandId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns((Brands?)null);

        _ = await _sut.Handle(new DeleteBrandCommand(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
