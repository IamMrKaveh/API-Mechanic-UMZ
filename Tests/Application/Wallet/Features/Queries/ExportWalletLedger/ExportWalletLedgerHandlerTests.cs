using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.ExportWalletLedger;
using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.ExportWalletLedger;

public sealed class ExportWalletLedgerHandlerTests
{
    private readonly IWalletQueryService _walletQueryService = Substitute.For<IWalletQueryService>();
    private readonly ExportWalletLedgerHandler _sut;

    public ExportWalletLedgerHandlerTests()
    {
        _sut = new ExportWalletLedgerHandler(_walletQueryService);
    }

    private static WalletLedgerEntryDto CreateEntry(
        Guid? id = null,
        Guid? walletId = null,
        Guid? userId = null,
        decimal amountDelta = 100_000m,
        decimal balanceAfter = 500_000m,
        string transactionType = "Credit",
        string referenceType = "TopUp",
        Guid? referenceId = null,
        string? description = "seed",
        DateTime? createdAt = null,
        bool isAdminAdjustment = false) =>
        new(
            id ?? Guid.NewGuid(),
            walletId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            amountDelta,
            balanceAfter,
            transactionType,
            referenceType,
            referenceId ?? Guid.NewGuid(),
            description,
            createdAt ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            isAdminAdjustment);

    [Fact]
    public async Task Handle_WhenFormatIsCsv_ReturnsCsvContentTypeAndExtension()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto> { CreateEntry(userId: userId) };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldEndWith(".csv");
        result.Value.FileContent.ShouldNotBeNull();
        result.Value.FileContent.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WhenFormatIsJson_ReturnsJsonContentTypeAndSerializedEntries()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto>
        {
            CreateEntry(userId: userId, amountDelta: 250_000m, transactionType: "Debit")
        };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId, Format: "json");

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe("application/json");
        result.Value.FileName.ShouldEndWith(".json");
        var json = Encoding.UTF8.GetString(result.Value.FileContent);
        json.ShouldContain("Debit");
        var parsed = JsonSerializer.Deserialize<List<WalletLedgerEntryDto>>(result.Value.FileContent);
        parsed.ShouldNotBeNull();
        parsed!.Count.ShouldBe(1);
        parsed[0].AmountDelta.ShouldBe(250_000m);
    }

    [Theory]
    [InlineData("JSON", "application/json", ".json")]
    [InlineData("Json", "application/json", ".json")]
    [InlineData("CSV", "text/csv", ".csv")]
    [InlineData("csv", "text/csv", ".csv")]
    public async Task Handle_WhenFormatIsProvidedInAnyCase_ResolvesFormatCaseInsensitively(
        string format,
        string expectedContentType,
        string expectedExtension)
    {
        var userId = Guid.NewGuid();
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<WalletLedgerEntryDto>());

        var query = new ExportWalletLedgerQuery(userId, Format: format);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe(expectedContentType);
        result.Value.FileName.ShouldEndWith(expectedExtension);
    }

    [Fact]
    public async Task Handle_WhenEntriesEmpty_ReturnsCsvWithOnlyHeaderRow()
    {
        var userId = Guid.NewGuid();
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<WalletLedgerEntryDto>());

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var csv = Encoding.UTF8.GetString(result.Value.FileContent);
        var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(1);
        lines[0].ShouldContain("Id,WalletId,UserId,AmountDelta,BalanceAfter,TransactionType,ReferenceType,ReferenceId,Description,CreatedAt,IsAdminAdjustment");
    }

    [Fact]
    public async Task Handle_WhenDescriptionContainsCommaOrQuote_EscapesCsvFieldCorrectly()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto>
        {
            CreateEntry(userId: userId, description: "hello, \"world\"")
        };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var csv = Encoding.UTF8.GetString(result.Value.FileContent);
        csv.ShouldContain("\"hello, \"\"world\"\"\"");
    }

    [Fact]
    public async Task Handle_WhenDescriptionIsNull_WritesEmptyCsvField()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto>
        {
            CreateEntry(userId: userId, description: null)
        };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var csv = Encoding.UTF8.GetString(result.Value.FileContent);
        csv.ShouldNotContain("\"\"");
    }

    [Fact]
    public async Task Handle_WhenAmountsWrittenInCsv_UsesInvariantCultureFormatting()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto>
        {
            CreateEntry(userId: userId, amountDelta: 1234.56m, balanceAfter: 9876.54m)
        };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var csv = Encoding.UTF8.GetString(result.Value.FileContent);
        csv.ShouldContain("1234.56");
        csv.ShouldContain("9876.54");
    }

    [Fact]
    public async Task Handle_WhenIsAdminAdjustmentTrue_WritesTrueLiteralInCsv()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto>
        {
            CreateEntry(userId: userId, isAdminAdjustment: true)
        };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var csv = Encoding.UTF8.GetString(result.Value.FileContent);
        csv.TrimEnd().ShouldEndWith("true");
    }

    [Fact]
    public async Task Handle_WhenIsAdminAdjustmentFalse_WritesFalseLiteralInCsv()
    {
        var userId = Guid.NewGuid();
        var entries = new List<WalletLedgerEntryDto>
        {
            CreateEntry(userId: userId, isAdminAdjustment: false)
        };
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(entries);

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var csv = Encoding.UTF8.GetString(result.Value.FileContent);
        csv.TrimEnd().ShouldEndWith("false");
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesFilterFromQueryToService()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        WalletLedgerFilter? capturedFilter = null;
        UserId? capturedUserId = null;
        _walletQueryService
            .ExportLedgerAsync(Arg.Do<UserId>(u => capturedUserId = u), Arg.Do<WalletLedgerFilter>(f => capturedFilter = f), false, Arg.Any<CancellationToken>())
            .Returns(new List<WalletLedgerEntryDto>());

        var query = new ExportWalletLedgerQuery(
            userId,
            FromDate: from,
            ToDate: to,
            TransactionType: "Debit",
            MinAmount: 1_000m,
            MaxAmount: 500_000m,
            SearchTerm: "order",
            Format: "csv",
            MaxRows: 5_000);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        capturedFilter.ShouldNotBeNull();
        capturedFilter!.FromDate.ShouldBe(from);
        capturedFilter.ToDate.ShouldBe(to);
        capturedFilter.TransactionType.ShouldBe("Debit");
        capturedFilter.MinAmount.ShouldBe(1_000m);
        capturedFilter.MaxAmount.ShouldBe(500_000m);
        capturedFilter.SearchTerm.ShouldBe("order");
        capturedFilter.MaxRows.ShouldBe(5_000);
        capturedUserId.ShouldNotBeNull();
        capturedUserId!.Value.ShouldBe(userId);
    }

    [Fact]
    public async Task Handle_WhenCalled_AlwaysPassesIncludeInactiveUsersFalse()
    {
        var userId = Guid.NewGuid();
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletLedgerEntryDto>());

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _walletQueryService.Received(1).ExportLedgerAsync(
            Arg.Any<UserId>(),
            Arg.Any<WalletLedgerFilter>(),
            false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ThrowsDomainException()
    {
        var query = new ExportWalletLedgerQuery(Guid.Empty);

        var act = async () => await _sut.Handle(query, CancellationToken.None);

        await act.ShouldThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PassesTokenToService()
    {
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<WalletLedgerEntryDto>());

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, cts.Token);

        result.ShouldBeSuccess();
        await _walletQueryService.Received(1).ExportLedgerAsync(
            Arg.Any<UserId>(),
            Arg.Any<WalletLedgerFilter>(),
            false,
            cts.Token);
    }

    [Fact]
    public async Task Handle_WhenCalled_FileNameContainsUserIdAndUtcTimestamp()
    {
        var userId = Guid.NewGuid();
        _walletQueryService
            .ExportLedgerAsync(Arg.Any<UserId>(), Arg.Any<WalletLedgerFilter>(), false, Arg.Any<CancellationToken>())
            .Returns(new List<WalletLedgerEntryDto>());

        var query = new ExportWalletLedgerQuery(userId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.FileName.ShouldStartWith("wallet_ledger_");
        result.Value.FileName.ShouldContain(userId.ToString("N"));
    }
}
