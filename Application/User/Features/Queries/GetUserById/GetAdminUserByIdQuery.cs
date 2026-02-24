namespace Application.User.Features.Queries.GetUserById;

public record GetAdminUserByIdQuery(int Id) : IRequest<ServiceResult<UserProfileDto?>>;