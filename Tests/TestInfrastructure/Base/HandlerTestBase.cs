namespace Tests.TestInfrastructure.Base;

/// <summary>
/// Shared base for Application-layer unit tests (handlers, validators, behaviors, event handlers).
/// Centralizes the four most-duplicated substitutes so individual test classes no longer need
/// <c>Substitute.For&lt;IDateTimeProvider&gt;</c>, <c>IAuditService</c>, <c>ICurrentUserService</c>
/// or <c>IUnitOfWork</c> field declarations.
/// xUnit creates a new test-class instance per test, so these substitutes are fresh for every test.
/// </summary>
public abstract class HandlerTestBase
{
    protected IDateTimeProvider DateTimeProvider { get; } = Substitute.For<IDateTimeProvider>();

    protected IAuditService AuditService { get; } = Substitute.For<IAuditService>();

    protected ICurrentUserService CurrentUserService { get; } = Substitute.For<ICurrentUserService>();

    protected IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    protected HandlerTestBase()
    {
        // Sensible neutral defaults. Individual tests override per-test as before;
        // NSubstitute keeps the last Returns configuration, so overrides win.
        DateTimeProvider.UtcNow.Returns(_ => DateTime.UtcNow);
        DateTimeProvider.Today.Returns(_ => DateOnly.FromDateTime(DateTime.UtcNow));

        CurrentUserService.UserId.Returns((Guid?)null);
        CurrentUserService.SessionId.Returns((Guid?)null);
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.IsAdmin.Returns(false);
        CurrentUserService.IpAddress.Returns((string?)null);
        CurrentUserService.UserAgent.Returns((string?)null);
        CurrentUserService.GuestToken.Returns((string?)null);
        CurrentUserService.FrontendBaseUrl.Returns("https://localhost");
    }

    /// <summary>Freezes clock at a fixed instant (stubs both UtcNow and Today consistently).</summary>
    protected DateTime SetUtcNow(DateTime utcNow)
    {
        var fixedNow = utcNow.Kind == DateTimeKind.Utc ? utcNow : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        DateTimeProvider.UtcNow.Returns(fixedNow);
        DateTimeProvider.Today.Returns(DateOnly.FromDateTime(fixedNow));
        return fixedNow;
    }

    protected DateTime SetUtcNow(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => SetUtcNow(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc));

    /// <summary>Restores a live clock (each access returns current time).</summary>
    protected void UseLiveUtcNow()
    {
        DateTimeProvider.UtcNow.Returns(_ => DateTime.UtcNow);
        DateTimeProvider.Today.Returns(_ => DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>Stubs an authenticated user with optional session/ip context.</summary>
    protected Guid AuthenticateAs(
        Guid? userId = null,
        Guid? sessionId = null,
        bool isAdmin = false,
        string? ipAddress = "127.0.0.1",
        string? userAgent = "TestAgent")
    {
        var id = userId ?? Guid.NewGuid();
        CurrentUserService.UserId.Returns(id);
        CurrentUserService.SessionId.Returns(sessionId);
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.IsAdmin.Returns(isAdmin);
        CurrentUserService.IpAddress.Returns(ipAddress);
        CurrentUserService.UserAgent.Returns(userAgent);
        return id;
    }

    /// <summary>Stubs an anonymous (unauthenticated) user.</summary>
    protected void AuthenticateAsAnonymous(string? ipAddress = null, string? userAgent = null)
    {
        CurrentUserService.UserId.Returns((Guid?)null);
        CurrentUserService.SessionId.Returns((Guid?)null);
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.IsAdmin.Returns(false);
        CurrentUserService.IpAddress.Returns(ipAddress);
        CurrentUserService.UserAgent.Returns(userAgent);
    }

    /// <summary>
    /// Configures <see cref="IUnitOfWork.ExecuteStrategyAsync{T}"/> to execute the operation inline
    /// (pass-through) for the given <typeparamref name="T"/>. Call per T used by the handler under test.
    /// </summary>
    protected void SetupUnitOfWorkPassthrough<T>()
    {
        UnitOfWork
            .ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<T>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var operation = ci.Arg<Func<CancellationToken, Task<T>>>();
                return await operation(ci.Arg<CancellationToken>());
            });
    }

    /// <summary>Pass-through for the most common <c>int</c> strategy result (bulk operations).</summary>
    protected void SetupUnitOfWorkIntPassthrough() => SetupUnitOfWorkPassthrough<int>();
}
