using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace states.Mongo.Documents
{
    /// <summary>
    /// Хранит resume token change stream для возобновления после рестарта.
    /// </summary>
    public class ChangeStreamCheckpoint
    {
        [BsonId]
        public string Id { get; set; } = default!;

        [BsonElement("resumeToken")]
        public BsonDocument ResumeToken { get; set; } = default!;
    }
}
