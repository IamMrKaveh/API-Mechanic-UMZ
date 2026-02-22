namespace Application.Audit.Contracts;

public interface IAuditMaskingService
{
    string MaskSensitiveData(
        string input
        );

    string MaskDetails(
        string details
        );
}