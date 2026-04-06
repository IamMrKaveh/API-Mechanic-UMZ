using Application.Common.Results;

namespace Application.Common.Interfaces;

public interface IHttpResultMapper
{
    int GetStatusCode(ServiceResult result);
}