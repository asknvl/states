using Confluent.Kafka;
using MongoDB.Bson;
using MongoDB.Driver;
using states.Mongo.Documents;

namespace states.Mongo
{
    public sealed class MongoContext
    {
        public IMongoCollection<FunnelDocument> Funnels { get; }
        public IMongoCollection<FunnelLeadState> LeadStates { get; }
        public IMongoCollection<ActionTaskDocument> ActionTasks { get; }

        public MongoContext(IMongoDatabase database)
        {
            Funnels = database.GetCollection<FunnelDocument>("funnels");
            LeadStates = database.GetCollection<FunnelLeadState>("lead_states");
            ActionTasks = database.GetCollection<ActionTaskDocument>("action_tasks");
        }
    }
}
