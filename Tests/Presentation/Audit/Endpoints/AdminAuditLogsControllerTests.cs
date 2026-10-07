using Application.Audit.Features.Queries.ExportAuditLogs;
using Application.Audit.Features.Queries.GetAuditLogById;
using Application.Audit.Features.Queries.GetAuditLogs;
using Application.Audit.Features.Queries.GetAuditStatistics;
using Application.Audit.Features.Queries.VerifyAuditIntegrity;
using Application.Audit.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Audit.Endpoints;
using Presentation.Audit.Requests;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Audit.Endpoints;

public class AdminAuditLogsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminAuditLogsController _controller;

    public AdminAuditLogsControllerTests()
    {
        _controller = new AdminAuditLogsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAuditLogs_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetAuditLogsRequest(Page: 1, PageSize: 50);
        var query = new GetAuditLogsQuery(null, null, null, null, null, null, null, null, 1, 50, "CreatedAt", true);
        var paged = new PaginatedResult<AuditLogDto>
        {
            Items = [new AuditLogDto { Id = Guid.NewGuid(), EventType = "Login", Action = "UserLogin" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 50
        };

        _mapper.Map<GetAuditLogsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAuditLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AuditLogDto>>.Success(paged));

        // Act
        var result = await _controller.GetAuditLogs(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<AuditLogDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetAuditLogs_WhenFailure_MapsToErrorStatus()
    {
        // Arrange
        var request = new GetAuditLogsRequest();
        var query = new GetAuditLogsQuery(null, null, null, null, null, null, null, null, 1, 50, "CreatedAt", true);

        _mapper.Map<GetAuditLogsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAuditLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AuditLogDto>>.NotFound());

        // Act
        var result = await _controller.GetAuditLogs(request, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetById_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new AuditLogDetailDto { Id = id, EventType = "Login", Action = "UserLogin" };

        _mediator.Send(Arg.Is<GetAuditLogByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AuditLogDetailDto>.Success(expected));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<AuditLogDetailDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task VerifyIntegrity_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new AuditIntegrityResultDto { Id = id, IsValid = true };

        _mediator.Send(Arg.Is<VerifyAuditIntegrityQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AuditIntegrityResultDto>.Success(expected));

        // Act
        var result = await _controller.VerifyIntegrity(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<AuditIntegrityResultDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task GetStatistics_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetAuditStatisticsRequest(null, null);
        var query = new GetAuditStatisticsQuery(null, null);
        var expected = new AuditStatisticsDto { TotalLogs = 10 };

        _mapper.Map<GetAuditStatisticsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAuditStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AuditStatisticsDto>.Success(expected));

        // Act
        var result = await _controller.GetStatistics(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<AuditStatisticsDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.TotalLogs.ShouldBe(10);
    }

    [Fact]
    public async Task ExportCsv_BuildsQueryWithCsvFormat_AndReturnsOk()
    {
        // Arrange
        var request = new ExportAuditLogsRequest(MaxRows: 100);
        var expected = new ExportAuditLogsResult([], "audit.csv", "text/csv");

        _mediator.Send(
                Arg.Is<ExportAuditLogsQuery>(q => q.Format == "csv" && q.MaxRows == 100),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ExportAuditLogsResult>.Success(expected));

        // Act
        var result = await _controller.ExportCsv(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<ExportAuditLogsResult>>();
        body.Success.ShouldBeTrue();
        body.Data!.FileName.ShouldBe("audit.csv");
    }

    [Fact]
    public async Task ExportJson_BuildsQueryWithJsonFormat_AndReturnsOk()
    {
        // Arrange
        var request = new ExportAuditLogsRequest(MaxRows: 50);
        var expected = new ExportAuditLogsResult([], "audit.json", "application/json");

        _mediator.Send(
                Arg.Is<ExportAuditLogsQuery>(q => q.Format == "json" && q.MaxRows == 50),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ExportAuditLogsResult>.Success(expected));

        // Act
        var result = await _controller.ExportJson(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<ExportAuditLogsResult>>();
        body.Success.ShouldBeTrue();
        body.Data!.FileName.ShouldBe("audit.json");
    }

    [Fact]
    public void AdminAuditLogsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminAuditLogsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminAuditLogsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminAuditLogsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/audit-logs");
    }

    [Theory]
    [InlineData(nameof(AdminAuditLogsController.GetAuditLogs), null)]
    [InlineData(nameof(AdminAuditLogsController.GetById), "{id:guid}")]
    [InlineData(nameof(AdminAuditLogsController.VerifyIntegrity), "{id:guid}/integrity")]
    [InlineData(nameof(AdminAuditLogsController.GetStatistics), "statistics")]
    [InlineData(nameof(AdminAuditLogsController.ExportCsv), "export/csv")]
    [InlineData(nameof(AdminAuditLogsController.ExportJson), "export/json")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminAuditLogsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
