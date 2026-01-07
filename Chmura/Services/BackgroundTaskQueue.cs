using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Chmura.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, Task>> _queue;

        public BackgroundTaskQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(options);
        }

        public void EnqueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));
            // Nie czekamy tu — Writer.WaitToWriteAsync bêdzie blokowaæ jeœli kana³ pe³ny.
            _queue.Writer.TryWrite(workItem);
        }

        public async ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            return workItem;
        }
    }
}