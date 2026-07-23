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

        [BsonElement("campaignId")]
        public Guid? CampaignId { get; set; }

        [BsonElement("campaignName")]
        public string? CampaignName { get; set; }

        [BsonElement("sourceId")]
        public string? SourceId { get; set; }

        [BsonElement("sourceName")]
        public string? SourceName { get; set; }

        [BsonElement("leadId")]
        public string LeadId { get; set; }

        [BsonElement("funnelId")]    
        public Guid? FunnelId { get; set; }

        [BsonElement("funnelName")]
        public string? FunnelName { get; set; }

        [BsonElement("flowId")]       
        public Guid? FlowId { get; set; }

        [BsonElement("flowName")]
        public string? FlowName { get; set; }

        [BsonElement("nodeId")]       
        public Guid? NodeId { get; set; }

        [BsonElement("nodeLabel")]
        public string? NodeLabel { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public LeadFunnelStatus Status {  get; set; }

        [BsonElement("preBlockStatus")]
        [BsonRepresentation(BsonType.String)]
        public LeadFunnelStatus? PreBlockStatus { get; set; }

        [BsonElement("isInputTranslatorOn")]
        public bool IsInputTranslatorOn { get; set; }

        [BsonElement("isOutputTranslatorOn")]
        public bool IsOutputTranslatorOn { get; set; }

        [BsonElement("photoRecognition")]
        [BsonRepresentation(BsonType.String)]
        public RecognitionType PhotoRecognition { get; set; } = RecognitionType.Enabled;

        [BsonElement("videoRecognition")]
        [BsonRepresentation(BsonType.String)]
        public RecognitionType VideoRecognition { get; set; } = RecognitionType.Enabled;

        [BsonElement("voiceRecognition")]
        [BsonRepresentation(BsonType.String)]
        public RecognitionType VoiceRecognition { get; set; } = RecognitionType.Enabled;

        [BsonElement("version")]
        public long Version { get; set; }

        [BsonElement("tags")]
        public List<TagDocument> Tags { get; set; } = [];

        [BsonElement("statesLog")]
        public List<StateLogEntry> StatesLog { get; set; } = [];

        // Id успешно отправленных пушей лида, не привязан к ноде/визиту — используется, чтобы не
        // отправлять повторно уже доставленный пуш при повторном входе в ноду. Заполняется через
        // $addToSet (атомарно, без дублей, без гонок при конкурентном завершении разных пушей).
        [BsonElement("pushes")]
        public List<Guid> Pushes { get; set; } = [];

        [BsonElement("startParameter")]
        public string? StartParameter { get; set; }

        [BsonElement("totalDepositAmount")]
        public decimal TotalDepositAmount { get; set; }

        [BsonElement("firstDepositAmount")]
        public decimal FirstDepositAmount { get; set; }

        [BsonElement("lastDepositAmount")]
        public decimal LastDepositAmount { get; set; }

        [BsonElement("depositCount")]
        public int DepositCount { get; set; }

        [BsonElement("currencyCode")]
        public string CurrencyCode { get; set; } = string.Empty;

        // Произвольные параметры, накопленные из постбеков трекера (по leadId). Новые постбеки
        // дополняют словарь, а не заменяют его целиком — см. LeadStateRepository.MergePostbackParameters.
        [BsonElement("postbackParameters")]
        public Dictionary<string, string> PostbackParameters { get; set; } = [];

        // Момент первого входящего сообщения лида. Пока null — событие Contact ещё не записано;
        // выставляется атомарно один раз (см. LeadStateRepository.TryMarkFirstContact).
        [BsonElement("firstContactAt")]
        public DateTime? FirstContactAt { get; set; }

        // Момент последнего входящего сообщения лида. Обновляется на каждый message signal до
        // захвата лида (см. HandleIncomingSignal), поэтому актуален даже когда сам сигнал
        // проигнорирован. По нему PushWorkerService сдвигает inactivity-пуши AiReply-нод.
        [BsonElement("lastIncomingAt")]
        public DateTime? LastIncomingAt { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //[BsonElement("depSum")]
        //public Decimal DepSum { get; set; } = 0;

        //public 
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
