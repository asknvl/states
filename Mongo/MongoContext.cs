using Confluent.Kafka;
using MongoDB.Bson;
using MongoDB.Driver;
using states.Mongo.Documents;

namespace states.Mongo
{
    public sealed class MongoContext
    {
        public IMongoCollection<Funnel> Funnels { get; }        
        public IMongoCollection<FunnelLeadState> LeadStates {  get; }


        public MongoContext(IMongoDatabase database)
        {
            Funnels = database.GetCollection<Funnel>("funnels");
            LeadStates = database.GetCollection<FunnelLeadState>("lead_states");
        }
    }
}
