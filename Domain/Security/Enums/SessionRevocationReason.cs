namespace Domain.Security.Enums;

public enum SessionRevocationReason
{
    UserRequested = 1,
    AdminRevoked = 2,
    SecurityConcern = 3,
    PasswordChanged = 4,
    AccountDeactivated = 5,
    Expired = 6,
    AllSessionsRevoked = 7
}