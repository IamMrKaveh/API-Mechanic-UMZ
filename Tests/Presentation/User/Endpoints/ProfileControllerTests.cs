using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
using Application.User.Features.Commands.ChangePassword;
using Application.User.Features.Commands.ChangePhoneNumber;
using Application.User.Features.Commands.DeactivateAccount;
using Application.User.Features.Commands.UpdateProfile;
using Application.User.Features.Queries.GetCurrentUser;
using Application.User.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.User.Endpoints;
using Presentation.User.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.User.Endpoints;

public class ProfileControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _controller = new ProfileController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetProfile_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new UserProfileDto { Id = Guid.NewGuid(), PhoneNumber = "09123456789" };

        _mediator.Send(Arg.Any<GetCurrentUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserProfileDto>.Success(expected));

        // Act
        var result = await _controller.GetProfile(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<UserProfileDto>>();
        body.Data!.PhoneNumber.ShouldBe("09123456789");
    }

    [Fact]
    public async Task GetMyReviews_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<ProductReviewDto>
        {
            Items = [new ProductReviewDto { Id = Guid.NewGuid(), Rating = 5 }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetUserReviewsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductReviewDto>>.Success(paged));

        // Act
        var result = await _controller.GetMyReviews();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetUserReviewsQuery>(q => q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new UpdateProfileRequest("Ali", "Rezaei");
        var expected = new UserProfileDto { Id = Guid.NewGuid(), FirstName = "Ali" };

        _mediator.Send(Arg.Any<UpdateProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserProfileDto>.Success(expected));

        // Act
        var result = await _controller.UpdateProfile(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateProfileCommand>(c => c.FirstName == "Ali" && c.LastName == "Rezaei"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAccount_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<DeactivateAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteAccount(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<DeactivateAccountCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePassword_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new ChangePasswordRequest("old", "new", "new");

        _mediator.Send(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ChangePasswordCommand>(c => c.CurrentPassword == "old" && c.NewPassword == "new"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePhoneNumber_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new ChangePhoneNumberRequest("09987654321", "123456");

        _mediator.Send(Arg.Any<ChangePhoneNumberCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ChangePhoneNumber(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ChangePhoneNumberCommand>(c => c.NewPhoneNumber == "09987654321" && c.OtpCode == "123456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ProfileController_HasAuthorizeAttribute()
    {
        typeof(ProfileController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ProfileController_HasRouteAttribute()
    {
        var routeAttr = typeof(ProfileController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/profile");
    }

    [Theory]
    [InlineData(nameof(ProfileController.GetProfile), null)]
    [InlineData(nameof(ProfileController.GetMyReviews), "reviews")]
    [InlineData(nameof(ProfileController.UpdateProfile), null)]
    [InlineData(nameof(ProfileController.DeleteAccount), null)]
    [InlineData(nameof(ProfileController.ChangePassword), "password")]
    [InlineData(nameof(ProfileController.ChangePhoneNumber), "phone")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(ProfileController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
