using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chmura.Services
{
    public interface IBackgroundTaskQueue
    {
        void EnqueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
        ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}