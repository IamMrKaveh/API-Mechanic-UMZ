using SharedKernel.Constants;

namespace Application.Common.Formatting;

public static class UserFullNameFormatter
{
    public static string Format(string? firstName, string? lastName)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();
        var full = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(full) ? UserConstants.DeletedUserDisplayName : full;
    }

    public static string Format(Domain.User.Aggregates.User? user)
    {
        if (user?.FullName is null)
            return UserConstants.DeletedUserDisplayName;

        return Format(user.FullName.FirstName, user.FullName.LastName);
    }
}
