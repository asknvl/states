using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents
{
    public class LeadDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonElement("tenantId")]       
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid TenantId { get; set; }

        [BsonElement("spaceId")]    
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid SpaceId { get; set; }

        [BsonElement("botId")]       
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid BotId { get; set; }

        [BsonElement("funnelId")]    
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid FunnelId { get; set; }

        [BsonElement("flowId")]       
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid FlowId { get; set; }

        [BsonElement("nodeId")]       
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid NodeId { get; set; }

        [BsonElement("statesLog")]
        public List<StateLogEntry> StatesLog { get; set; } = [];
    }

    public class StateLogEntry
    {
        [BsonElement("nodeId")]        
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid NodeId { get; set; }

        [BsonElement("enteredAt")]
        public DateTime EnteredAt { get; set; }

        [BsonElement("leftAt")]
        public DateTime? LeftAt { get; set; }

        [BsonElement("exitEdgeId")]        
        public string? ExitEdgeId { get; set; }

        [BsonElement("actions")]        
        public List<ActionStatusEntry> ActionsLog { get; set; } = [];
    }

    public class ActionStatusEntry
    {
        public required string Id { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public ActionType Type { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public ActionStatus Status { get; set; }

        [BsonElement("timeStamp")]
        public DateTime Timestamp { get; set; }
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
