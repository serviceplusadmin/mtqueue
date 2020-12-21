using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTQueue
{
    public class MTQueueDatabase<T> where T : MTQueueItem
    {
        public virtual async Task<bool> AddQueueItem(T item)
        {
            return false;
        }
        public virtual async Task<IEnumerable<T>> GetQueueItems(int count)
        {
            return null;
        }
        public virtual async Task<bool> RemoveQueueItem(T item)
        {
            return false;
        }
    }
}