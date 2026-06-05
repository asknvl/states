using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Mongo.Documents.Edges;
using states.Mongo.Documents.Nodes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents
{
    public class FunnelDocument
    {
        [BsonId]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonElement("tenantId")]
        public Guid TenantId { get; init; }

        [BsonElement("spaceId")]
        public Guid SpaceId { get; init; }       

        [BsonElement("name")]
        public string Name { get; init; } = default!;

        [BsonElement("description")]
        public string? Description { get; init; }

        [BsonElement("tags")]
        public List<TagDocument> Tags { get; init; } = [];

        [BsonElement("flows")]
        public List<FlowDocument> Flows { get; init; } = [];

        [BsonElement("presetsFolderId")]
        public Guid PresetsFolderId { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("isInputTranslatorOn")]
        public bool IsInputTranslatorOn { get; set; }

        [BsonElement("isOutputTranslatorOn")]
        public bool IsOutputTranslatorOn { get; set; }

        [BsonElement("photoRecognition")]
        [BsonRepresentation(BsonType.String)]
        public RecognitionType PhotoRecognition { get; set; } = RecognitionType.Skip;

        [BsonElement("videoRecognition")]
        [BsonRepresentation(BsonType.String)]
        public RecognitionType VideoRecognition { get; set; } = RecognitionType.Skip;

        [BsonElement("voiceRecognition")]
        [BsonRepresentation(BsonType.String)]
        public RecognitionType VoiceRecognition { get; set; } = RecognitionType.Skip;

        [BsonElement("readDelay")]
        public int ReadDelay { get; set; } = 5;

        [BsonElement("replyDelay")]
        public int ReplyDelay { get; set; } = 10;


        [BsonElement("AiRouterModelPresetId")]
        public Guid AiRouterModelPresetId { get; set; } 

        [BsonElement("AiReplyModelPresetId")]
        public Guid AiReplyModelPresetId { get; set; }

        [BsonElement("AiReplyTemperature")]
        public double AiReplyTemperature { get; set; }


        [BsonElement("GlobalLegend")]
        public string? GlobalLegend { get; set; }

        [BsonElement("Restrictions")]
        public string? Restrictions { get; set; }

        [BsonElement("ResponseStyle")]
        public string? ResponseStyle { get; set; }

        [BsonElement("variables")]
        public List<Variable> Variables { get; set; } = [];
    }

    public class FlowDocument
    {
        public Guid Id { get; set; }

        [BsonElement("name")]
        public required string Name { get; set; }

        [BsonElement("nodes")]
        public List<NodeDocument> Nodes { get; set; } = [];

        [BsonElement("edges")]
        public List<EdgeDocument> Edges { get; set; } = [];
    }

    public class TagDocument
    {
        [BsonElement("id")]
        public Guid Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = default!;
    }

    public class Variable
    {
        [BsonElement("id")]
        public Guid Id { get; set; }

        [BsonElement("macros")]
        public string Macros { get; set; }

        [BsonElement("value")]
        public string Value { get; set; }
    }
}
