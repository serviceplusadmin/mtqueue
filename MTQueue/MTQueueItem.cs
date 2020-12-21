using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MTQueue
{
    public class MTQueueItem
    {
        [BsonId]
        public BsonObjectId _id { get; set; }
        public string ItemId { get; set; }
        public bool Completed { get; set; }
    }
}