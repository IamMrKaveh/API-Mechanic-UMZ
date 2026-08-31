using Application.Audit.Features.Queries.GetAuditLogById;
using Application.Audit.Features.Shared;

namespace Tests.Application.Audit.Features.Queries.GetAuditLogById;

public class GetAuditLogByIdHandlerTests
{
    private readonly IAuditQueryService _queryService = Substitute.For<IAuditQueryService>();
    private readonly GetAuditLogByIdHandler _sut;

    public GetAuditLogByIdHandlerTests()
    {
        _sut = new GetAuditLogByIdHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ReturnsNotFound()
    {
        var query = new GetAuditLogByIdQuery(Guid.Empty);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_DoesNotCallQueryService()
    {
        var query = new GetAuditLogByIdQuery(Guid.Empty);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _queryService
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((AuditLogDetailDto?)null);

        var query = new GetAuditLogByIdQuery(id);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCode.NotFound);
        result.Error.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WhenLogExists_ReturnsSuccessWithDto()
    {
        var id = Guid.NewGuid();
        var dto = new AuditLogDetailDto
        {
            Id = id,
            UserId = Guid.NewGuid(),
            UserName = "alice",
            EventType = "Security",
            Action = "Login",
            Details = "ok",
            IpAddress = "10.0.0.1",
            UserAgent = "xunit",
            EntityType = "User",
            EntityId = "u-1",
            CreatedAt = DateTime.UtcNow,
            IsArchived = false,
            ArchivedAt = null,
            IntegrityHash = "hash"
        };

        _queryService
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(dto);

        var query = new GetAuditLogByIdQuery(id);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_CallsQueryServiceExactlyOnce_WithProvidedId()
    {
        var id = Guid.NewGuid();
        _queryService
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new AuditLogDetailDto { Id = id });

        var query = new GetAuditLogByIdQuery(id);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();
        _queryService
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new AuditLogDetailDto { Id = id });

        var query = new GetAuditLogByIdQuery(id);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetByIdAsync(id, cts.Token);
    }
}
