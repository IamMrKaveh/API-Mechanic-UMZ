using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateUser;

public class UpdateUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateUserCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateUserCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);
        var adminId = UserId.From(request.CurrentUserId);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        if (!user.IsActive)
            return ServiceResult.Forbidden("حساب کاربری غیرفعال است و قابل ویرایش نیست.");

        var firstName = !string.IsNullOrEmpty(request.FirstName)
            ? request.FirstName
            : user.FullName.FirstName;

        var lastName = !string.IsNullOrEmpty(request.LastName)
            ? request.LastName
            : user.FullName.LastName;

        var fullName = FullName.Create(firstName, lastName);
        user.UpdateProfile(fullName, user.PhoneNumber);

        userRepository.Update(user);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            await auditService.LogAdminEventAsync(
                "UpdateUser",
                adminId,
                $"Updated profile for user {request.Id}");
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("اطلاعات کاربر توسط فرآیند دیگری تغییر کرده است.");
        }
    }
}