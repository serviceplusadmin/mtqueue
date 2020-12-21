using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MTQueue
{
    public class MTQueuePool<T> where T : MTQueueItem
    {
        internal MTQueueManager<T> Manager { get; set; }
        internal ConcurrentQueue<T> Queue { get; } = new ConcurrentQueue<T>();
        public int PoolSize => Queue.Count;
        public int PoolCapacity { get; internal set; } = 10;
        internal int SleepTime { get; set; } = 100;
        internal Thread Pool { get; set; }
        internal Func<T, Task> Action { get; set; }
        internal void Initialize()
        {
            Pool = new Thread(Process);
            Pool.IsBackground = true;
            Pool.Priority = ThreadPriority.Lowest;
        }
        internal void Start()
        {
            Pool.Start();
        }
        private void Process()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (PoolSize < PoolCapacity)
                    {
                        var newPoolItems = (await Manager.Database.GetQueueItems(PoolCapacity - PoolSize));
                        foreach (T poolItem in newPoolItems)
                        {
                            Queue.Enqueue(poolItem);
                        }
                    }
                    if (Queue.TryDequeue(out var item))
                    {
                        await Manager.Database.RemoveQueueItem(item);
                        if (Action != null) await Action?.Invoke(item);
                        Manager.CompletedTasks[item.ItemId] = item;
                    }
                    await Task.Delay(SleepTime);
                }
            }).Wait();
        }
    }
}
