using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerDemo.Common
{
    public class Locker
    {
        private static readonly Dictionary<string, Locker> Lockers = new Dictionary<string, Locker>();

        public static Locker GetLocker(string id)
        {
            lock (Lockers)
            {
                if (!Lockers.ContainsKey(id))
                {
                    Lockers.Add(id, new Locker());
                }

                return Lockers[id];
            }
        }

        public class LockerInstance : IDisposable
        {
            private readonly Action _onDispose;

            public LockerInstance(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<LockerInstance> AcquireLock(CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            return new LockerInstance(() => _semaphoreSlim.Release());
        }
    }
}