using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(SendPresetActionTaskDocument))]
[BsonKnownTypes(typeof(ManageTagActionTaskDocument))]
[BsonKnownTypes(typeof(AiReplyActionTaskDocument))]
[BsonKnownTypes(typeof(AiRouterActionTaskDocument))]
[BsonKnownTypes(typeof(SendWebhookActionTaskDocument))]
[BsonKnownTypes(typeof(MarkReadActionTaskDocument))]
[BsonKnownTypes(typeof(SendTypingActionTaskDocument))]
public abstract class ActionTaskDocument
{
    [BsonId]
    public Guid Id { get; set; }

    [BsonElement("tenantId")]
    public Guid TenantId { get; set; }

    [BsonElement("spaceId")]
    public Guid SpaceId { get; set; }

    [BsonElement("leadStateId")]
    public Guid LeadStateId { get; set; }

    [BsonElement("funnelId")]
    public Guid FunnelId { get; set; }

    [BsonElement("flowId")]
    public Guid FlowId { get; set; }

    [BsonElement("nodeId")]
    public Guid NodeId { get; set; }

    [BsonElement("actionId")]
    public Guid ActionId { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ActionType Type { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ActionStatus Status { get; set; }

    [BsonElement("scheduledAt")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("claimedAt")]
    public DateTime? ClaimedAt { get; set; }

    // Проставляется при переходе в терминальный статус (Completed/Failed/Cancelled).
    // По этому полю работает TTL-индекс ttl_finished_action_tasks — Mongo сам удаляет
    // завершённые таски спустя retention-срок, активные (без поля) под TTL не попадают.
    [BsonElement("finishedAt")]
    [BsonIgnoreIfNull]
    public DateTime? FinishedAt { get; set; }

    [BsonElement("order")]
    public int Order { get; set; }

    // Сколько раз таска перепланировалась после transient-ошибки (0 — первая попытка).
    // Инкрементируется только в Reschedule и не сбрасывается — «в ретрае в моменте»:
    // { attempt: { $gt: 0 }, status: { $in: ["Pending", "InProgress"] } }.
    [BsonElement("attempt")]
    public int Attempt { get; set; }

    [BsonElement("isCritical")]
    public bool IsCritical { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    protected ActionTaskDocument(ActionType type, bool isCritical)
    {
        Type = type;
        IsCritical = isCritical;
        Status = ActionStatus.Pending;
    }
}

public sealed class SendPresetActionTaskDocument : ActionTaskDocument
{
    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    [BsonElement("presetId")]
    public Guid PresetId { get; set; }

    [BsonElement("needPin")]
    public bool NeedPin { get; set; }

    public SendPresetActionTaskDocument() : base(ActionType.SendPreset, isCritical: true) { }
}

public sealed class ManageTagActionTaskDocument : ActionTaskDocument
{
    [BsonElement("operation")]
    [BsonRepresentation(BsonType.String)]
    public TagOperation Operation { get; set; }

    [BsonElement("tagId")]
    public Guid TagId { get; set; }    

    [BsonElement("replacementTagId")]
    public Guid? ReplacementTagId { get; set; }    
    public ManageTagActionTaskDocument() : base(ActionType.ManageTag, isCritical: true) { }
}

public sealed class AiReplyActionTaskDocument : ActionTaskDocument
{
    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    [BsonElement("transitionAfterReply")]
    public bool TransitionAfterReply { get; set; }

    // Только для реактивных тасков (созданных по входящему сигналу или AiRouter no-match):
    // отправлять ответ, лишь если последнее сообщение контекста — входящее от юзера.
    // Защита от дубля: опоздавший/повторный сигнал на уже отвеченное сообщение создаёт таск,
    // который иначе сгенерировал бы тот же ответ ещё раз. Для проактивных тасков (вход в ноду
    // через переход) флаг false — там последним в контексте законно может быть наше сообщение.
    [BsonElement("replyOnlyIfLastIncoming")]
    public bool ReplyOnlyIfLastIncoming { get; set; }

    public AiReplyActionTaskDocument() : base(ActionType.AiReply, isCritical: true) { }
}

public sealed class AiRouterActionTaskDocument : ActionTaskDocument
{
    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    public AiRouterActionTaskDocument() : base(ActionType.AiRouter, isCritical: true) { }
}

// Реактивный таск «прочитать чат» — создаётся по входящему сигналу с задержкой ReadDelay воронки,
// чтобы прочитка выглядела натурально (лид видит «прочитано» до ответа, а не вместе с ним).
// Не входит в ActionsLog ноды и не влияет на переход по воронке.
public sealed class MarkReadActionTaskDocument : ActionTaskDocument
{
    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    public MarkReadActionTaskDocument() : base(ActionType.MarkRead, isCritical: false) { }
}

// Реактивный таск «показать печатает» — ставится вместе с AiReply по входящему сигналу:
// прочитка на ReadDelay, тайпинг незадолго до ответа, сам ответ на ReadDelay + ReplyDelay.
// Как и MarkRead, не входит в ActionsLog ноды и не влияет на переход по воронке.
// Длительность индикатора константная (TypingDefaults) — длина ответа заранее неизвестна.
public sealed class SendTypingActionTaskDocument : ActionTaskDocument
{
    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    // Сколько миллисекунд tgengine держит индикатор. Считается при планировании:
    // случайный lead (5-10с, обрезан по окну) + запас; 0 — фолбэк в экзекьюторе.
    [BsonElement("durationMs")]
    public int DurationMs { get; set; }

    public SendTypingActionTaskDocument() : base(ActionType.SendTyping, isCritical: false) { }
}

public sealed class SendWebhookActionTaskDocument : ActionTaskDocument
{
    [BsonElement("url")]
    public string Url { get; set; } = default!;

    [BsonElement("methodType")]
    [BsonRepresentation(BsonType.String)]
    public WebhookMethodType MethodType { get; set; }

    public SendWebhookActionTaskDocument() : base(ActionType.SendWebhook, isCritical: false) { }
}
