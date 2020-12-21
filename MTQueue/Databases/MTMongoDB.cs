using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTQueue.Databases.MongoDB
{
    public class MTMongoDB<T> : MTQueueDatabase<T> where T : MTQueueItem
    {
        public MTMongoDB(string connectionString, string databaseName = "mtqueue")
        {
            this.ConnectionString = connectionString;
            this.Client = new MongoClient(connectionString);
            this.DatabaseName = databaseName;
        }

        public string ConnectionString { get; }
        public string DatabaseName { get; }
        private MongoClient Client { get; }
        private IMongoDatabase Database { get; set; }
        private IMongoCollection<T> Collection { get; set; }

        private const string CollectionName = "queue";
        private ConcurrentQueue<T> Buffer { get; set; } = new ConcurrentQueue<T>();
        private async Task<bool> ConnectAsync()
        {
            if (Client == null) return false;
            try
            {
                if (this.Collection == null)
                {
                    this.Database = Client.GetDatabase(this.DatabaseName);
                    this.Collection = this.Database.GetCollection<T>(CollectionName);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public override async Task<bool> AddQueueItem(T item)
        {
            if (await ConnectAsync())
            {
                await this.Collection.InsertOneAsync(item);
            }
            return false;
        }
        public override async Task<bool> RemoveQueueItem(T item)
        {
            if (await ConnectAsync())
            {
                await this.Collection.DeleteOneAsync(Builders<T>.Filter.Eq("ItemId", item.ItemId));
            }
            return false;
        }
        public override async Task<IEnumerable<T>> GetQueueItems(int count)
        {
            if (Buffer.Count < 1)
            {
                if (await ConnectAsync())
                {
                    var cursor = await this.Collection.FindAsync<T>(Builders<T>.Filter.Eq("Completed", false));
                    foreach (var item in await cursor.ToListAsync())
                    {
                        Buffer.Enqueue(item);
                    }
                }
            }

            List<T> list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                if (Buffer.TryDequeue(out var _item))
                {
                    list.Add(_item);
                }
                else
                {
                    break;
                }
            }
            return list;
        }
    }
}
