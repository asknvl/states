using MongoDB.Driver;
using states.Mongo.Documents.Outbox;

namespace states.Mongo.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly IMongoCollection<OutboxDocument> collection;

    public OutboxRepository(MongoContext context)
    {
        collection = context.Outbox;
    }

    public async Task<List<OutboxDocument>> ClaimBatch(int size, TimeSpan claimTimeout, CancellationToken ct)
    {
        var staleThreshold = DateTime.UtcNow - claimTimeout;

        var filter = Builders<OutboxDocument>.Filter.Or(
            Builders<OutboxDocument>.Filter.Eq(x => x.ClaimedAt, null),
            Builders<OutboxDocument>.Filter.Lt(x => x.ClaimedAt, staleThreshold));

        var update = Builders<OutboxDocument>.Update.Set(x => x.ClaimedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<OutboxDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<OutboxDocument>.Sort.Ascending(x => x.CreatedAt)
        };

        var result = new List<OutboxDocument>(size);

        for (var i = 0; i < size; i++)
        {
            var doc = await collection.FindOneAndUpdateAsync(filter, update, options, ct);
            if (doc is null) break;
            result.Add(doc);
        }

        return result;
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        await collection.DeleteOneAsync(x => x.Id == id, ct);
    }
}
