using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using states.Logging;
using states.Services.Events;
using states.Services.Events.Producer.Payloads;

namespace states.Services.Events.Producer
{
    public class KafkaEventService : IEventService
    {
        private readonly IProducer<string, string> producer;
        private readonly ILogger logger;
        private readonly string leadStateEventsTopic;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public KafkaEventService(
            IProducer<string, string> producer,
            ILogger<KafkaEventService> logger,
            IConfiguration config)
        {
            this.producer = producer;
            this.logger = logger;
            leadStateEventsTopic = config["Kafka:Topics:LeadStateEvents"]
                ?? throw new InvalidOperationException("Kafka:Topics:LeadStateEvents not configured");
        }

        public async Task Publish<TPayload>(Event<TPayload> @event, CancellationToken ct = default)
        {
            var topic = ResolveTopic(@event);
            var key = ResolveKey(@event);

            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(@event, jsonOptions)
            };

            using (logger.Notifiacation(@event))
            {
                var result = await producer.ProduceAsync(topic, message, ct);

                logger.LogInformation(
                    "Kafka event published to {Topic}, partition={Partition}, offset={Offset}",
                    result.Topic,
                    result.Partition,
                    result.Offset);
            }
        }

        private string ResolveTopic<TPayload>(Event<TPayload> @event) => @event.Type switch
        {
            EventTypes.LeadStateCreated  => leadStateEventsTopic,
            EventTypes.LeadStatusChanged => leadStateEventsTopic,
            EventTypes.LeadNodeChanged   => leadStateEventsTopic,
            _ => throw new InvalidOperationException($"No topic mapping defined for event type '{@event.Type}'")
        };

        private static string ResolveKey<TPayload>(Event<TPayload> @event)
        {
            if (@event.Payload is LeadStateCreatedPayload createdPayload)
                return createdPayload.ChatId.ToString();

            if (@event.Payload is LeadStatusChangedPayload statusPayload)
                return statusPayload.ChatId.ToString();

            if (@event.Payload is LeadNodeChangedPayload nodePayload)
                return nodePayload.ChatId.ToString();

            return @event.Id.ToString();
        }
    }
}
