namespace EBAY.InventoryService.InventoryUpdate;

public sealed class InventoryUpdateExecutionLock
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async ValueTask<IDisposable?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new Releaser(gate);
    }

    private sealed class Releaser(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? gate = gate;

        public void Dispose()
        {
            Interlocked.Exchange(ref gate, null)?.Release();
        }
    }
}
