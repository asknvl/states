using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Documents
{
    public class FunnelLeadState
    {
        [BsonId]
        public Guid Id { get; set; }

        [BsonElement("tenantId")]       
        public Guid TenantId { get; set; }

        [BsonElement("spaceId")]    
        public Guid SpaceId { get; set; }

        [BsonElement("botId")]
        public Guid BotId { get; set; }

        [BsonElement("chatId")]
        public Guid ChatId { get; set; }

        [BsonElement("leadId")]
        public string LeadId { get; set; }

        [BsonElement("funnelId")]    
        public Guid FunnelId { get; set; }

        [BsonElement("flowId")]       
        public Guid FlowId { get; set; }

        [BsonElement("nodeId")]       
        public Guid NodeId { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public LeadFunnelStatus Status {  get; set; }

        [BsonElement("tags")]
        public List<Guid> Tags { get; set; } = [];

        [BsonElement("statesLog")]
        public List<StateLogEntry> StatesLog { get; set; } = [];
    }

    public class StateLogEntry
    {
        [BsonElement("nodeId")]        
        public Guid NodeId { get; set; }

        [BsonElement("enteredAt")]
        public DateTime EnteredAt { get; set; }

        [BsonElement("leftAt")]
        public DateTime? LeftAt { get; set; }

        [BsonElement("exitEdgeId")]
        public Guid? ExitEdgeId { get; set; }

        [BsonElement("actions")]        
        public List<ActionStatusEntry> ActionsLog { get; set; } = [];
    }

    public class ActionStatusEntry
    {
        [BsonElement("actionId")]
        public Guid ActionId { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public ActionType Type { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public ActionStatus Status { get; set; }

        [BsonElement("timeStamp")]
        public DateTime StatusChangedAt { get; set; }

        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    public enum ActionStatus
    {
        Pending,
        InProgress,
        Waiting,
        Completed,
        Failed,
        Cancelled
    }
}
