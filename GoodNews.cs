using System;
using System.Reflection.Metadata.Ecma335;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UkrainianSunbeamBot
{
    public class GoodNews
    {
        public ObjectId Id { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; }

        [BsonElement("Body")]
        public string Body { get; set; }

        [BsonElement("Source")]
        public string Source { get; set; }

        [BsonElement("Likes")]
        public int Likes { get; set; }
    }
}