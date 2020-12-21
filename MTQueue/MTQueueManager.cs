using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTQueue
{
    public class MTQueueManager<T> where T : MTQueueItem
    {
        public MTQueueDatabase<T> Database { get; set; }
        private ConcurrentBag<MTQueuePool<T>> Pools { get; set; } = new ConcurrentBag<MTQueuePool<T>>();
        public int MaxPoolCount { get; set; } = 10;
        public int MinPoolCount { get; set; } = 4;
        public int MaxPoolCapacity { get; set; } = 64;
        public int PoolSleepTime { get; set; } = 1;
        public int QueueSleepTime { get; set; } = 3;
        public int PoolWaitTime { get; set; } = 15;
        public Func<T, Task> Action { get; set; }
        internal ConcurrentDictionary<string, T> CompletedTasks { get; set; } = new ConcurrentDictionary<string, T>();
        public int CompletedTaskCount => CompletedTasks.Count;
        public IEnumerable<MTQueuePool<T>> QueuePools => Pools;

        public void Run()
        {
            for(int i = 0; i < MinPoolCount;i++)
            {
                CreateNewPool();
            }
        }

        public async Task<bool> Enqueue(T item)
        {
            try
            {
                if (Pools.Count < MinPoolCount)
                {
                    for (int i = 0; i < MinPoolCount - Pools.Count; i++)
                        CreateNewPool();
                }
                item.ItemId = Guid.NewGuid().ToString();
                await Database.AddQueueItem(item);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ResetCompletedTasks()
        {
            CompletedTasks.Clear();
        }

        private void CreateNewPool()
        {
            if (Pools.Count < MaxPoolCount)
            {
                MTQueuePool<T> pool = new MTQueuePool<T>();
                pool.Manager = this;
                pool.PoolCapacity = MaxPoolCapacity;
                pool.SleepTime = PoolSleepTime;
                pool.Action = Action;
                pool.Initialize();
                Pools.Add(pool);
                pool.Start();
            }
        }

        public async Task<T> Wait(T item)
        {
            try
            {
                string id = item.ItemId;

                T _item = null;
                do
                {
                    if (_item == null)
                    {
                        _item = CompletedTasks.ContainsKey(id) ? CompletedTasks[id] : null;
                    }
                    await Task.Delay(PoolWaitTime);
                } while (_item == null || !_item.Completed);

                CompletedTasks.TryRemove(_item.ItemId, out var _);
                
                return _item;
            }
            catch
            {
                return null;
            }
        }
    }
}
