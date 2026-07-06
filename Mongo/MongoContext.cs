using MongoDB.Driver;
using states.Mongo.Documents;
using states.Mongo.Documents.Folders;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Documents.Outbox;
using states.Mongo.Documents.TenantTags;

namespace states.Mongo
{
    public sealed class MongoContext
    {
        public IMongoCollection<FunnelDocument> Funnels { get; }
        public IMongoCollection<FunnelLeadState> LeadStates { get; }
        public IMongoCollection<ActionTaskDocument> ActionTasks { get; }
        public IMongoCollection<PushTaskDocument> PushTasks { get; }
        public IMongoCollection<OutboxDocument> Outbox { get; }
        public IMongoCollection<FolderDocument> Folders { get; }
        public IMongoCollection<TenantTag> TenantTags { get; set; }
        public IMongoCollection<LeadEventBaseDocument> LeadEvents { get; }

        public MongoContext(IMongoDatabase database)
        {
            Funnels = database.GetCollection<FunnelDocument>("funnels");
            LeadStates = database.GetCollection<FunnelLeadState>("lead_states");
            ActionTasks = database.GetCollection<ActionTaskDocument>("action_tasks");
            PushTasks = database.GetCollection<PushTaskDocument>("push_tasks");
            Outbox = database.GetCollection<OutboxDocument>("outbox");
            Folders = database.GetCollection<FolderDocument>("folders");
            TenantTags = database.GetCollection<TenantTag>("tenant_tags");
            LeadEvents = database.GetCollection<LeadEventBaseDocument>("lead_events");
        }
    }
}
