namespace Application.Common.Contracts;

public interface IUrlResolverService
{
    string? ResolveUrl(string? relativePath);
}