using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.UpdateUser;

public class UpdateUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IHtmlSanitizer htmlSanitizer) : IRequestHandler<UpdateUserCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly IHtmlSanitizer _htmlSanitizer = htmlSanitizer;

    public async Task<ServiceResult> Handle(
        UpdateUserCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, ct);
        if (user == null)
            return ServiceResult.NotFound("NotFound");

        if (user.IsDeleted)
            return ServiceResult.Forbidden("User account is deleted and cannot be modified.");

        user.UpdateName(
            !string.IsNullOrEmpty(request.UpdateRequest.FirstName)
                ? _htmlSanitizer.Sanitize(request.UpdateRequest.FirstName)
                : user.FirstName!,
            !string.IsNullOrEmpty(request.UpdateRequest.LastName)
                ? _htmlSanitizer.Sanitize(request.UpdateRequest.LastName)
                : user.LastName!
        );

        _userRepository.Update(user);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditService.LogAdminEventAsync(
                "UpdateUser",
                request.CurrentUserId,
                $"Updated profile for user {request.Id}");
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("User was modified by another process");
        }
    }
}