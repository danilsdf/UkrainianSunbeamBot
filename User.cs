using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UkrainianSunbeamBot
{
    public class User
    {
        public ObjectId Id { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("UsedNews")]
        public List<ObjectId> UsedNews { get; set; }
    }
}