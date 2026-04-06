namespace Application.Common.Contracts;

public interface IUrlResolverService
{
    string ResolveMediaUrl(string filePath);

    string ResolveAbsoluteUrl(string relativePath);
}