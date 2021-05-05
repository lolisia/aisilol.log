using System;
using System.Threading;
using System.Threading.Tasks;

namespace aisilol.log
{
    public sealed class SemaphoreLock
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private sealed class DisposeHandle : IDisposable
        {
            private SemaphoreSlim _semaphore { get; }
            private bool _disposed;

            public DisposeHandle(SemaphoreSlim semaphore) => _semaphore = semaphore; 
            
            public void Dispose()
            {
                if (_disposed)
                    return;
                
                _semaphore.Release();
                _disposed = true;
            }
        }
        
        public async Task<IDisposable> LockAsync()
        {
            await _semaphore.WaitAsync();
            return new DisposeHandle(_semaphore);
        }

        public IDisposable Lock()
        {
            _semaphore.Wait();
            return new DisposeHandle(_semaphore);
        }
    }
}