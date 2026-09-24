namespace EBAY.OrdersService.OrderImport;

public sealed class OrderImportExecutionLock
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<IDisposable?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        if (!await semaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new Lease(semaphore);
    }

    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
