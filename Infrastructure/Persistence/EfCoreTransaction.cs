using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using ITransaction = Application.Common.Interfaces.ITransaction;

namespace Infrastructure.Persistence;

internal sealed class EfCoreTransaction(IDbContextTransaction inner) : ITransaction
{
    private bool disposed;

    public IDbContextTransaction Inner => inner;

    public void Dispose()
    {
        if (disposed) return;
        inner.Dispose();
        disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await inner.DisposeAsync();
        disposed = true;
    }
}