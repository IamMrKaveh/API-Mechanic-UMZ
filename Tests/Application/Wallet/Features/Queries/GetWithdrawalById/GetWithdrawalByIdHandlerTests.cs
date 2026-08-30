using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetWithdrawalById;
using Application.Wallet.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.GetWithdrawalById;

public class GetWithdrawalByIdHandlerTests
{
    private readonly IWalletWithdrawalQueryService _queryService = Substitute.For<IWalletWithdrawalQueryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetWithdrawalByIdHandler _sut;

    public GetWithdrawalByIdHandlerTests()
    {
        _sut = new GetWithdrawalByIdHandler(_queryService, _currentUserService);
    }

    private static WalletWithdrawalRequestDto DtoFor(Guid userId, Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        userId,
        "Ali",
        1_000_000m,
        "IR12000000000000000000",
        "Ali",
        "for-rent",
        "Pending",
        null,
        null,
        DateTime.UtcNow,
        null,
        null,
        null,
        null);

    [Fact]
    public async Task Handle_WhenWithdrawalNotFound_ReturnsNotFound()
    {
        _queryService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequestDto?)null);

        var result = await _sut.Handle(new GetWithdrawalByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsAdmin_ReturnsSuccessRegardlessOfOwner()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var dto = DtoFor(ownerId);

        _currentUserService.IsAdmin.Returns(true);
        _currentUserService.UserId.Returns((Guid?)callerId);
        _queryService.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetWithdrawalByIdQuery(dto.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndUserIdIsNull_ReturnsForbidden()
    {
        var dto = DtoFor(Guid.NewGuid());

        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);
        _queryService.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetWithdrawalByIdQuery(dto.Id), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAccessesOtherUsersWithdrawal_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var dto = DtoFor(ownerId);

        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns((Guid?)callerId);
        _queryService.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetWithdrawalByIdQuery(dto.Id), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAccessesOwnWithdrawal_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var dto = DtoFor(userId);

        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns((Guid?)userId);
        _queryService.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetWithdrawalByIdQuery(dto.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
    }

    [Fact]
    public async Task Handle_PassesRequestIdToQueryServiceVerbatim()
    {
        var id = Guid.NewGuid();
        Guid capturedId = Guid.Empty;
        _queryService
            .GetByIdAsync(Arg.Do<Guid>(g => capturedId = g), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequestDto?)null);

        await _sut.Handle(new GetWithdrawalByIdQuery(id), CancellationToken.None);

        capturedId.ShouldBe(id);
    }
}
