
using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using states.Logging;

namespace states.Services.Events
{
    public class KafkaEventService : IEventService
    {
        private readonly IProducer<string, string> producer;
        private readonly ILogger logger;
        private readonly string chatEventsTopic;        

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

            chatEventsTopic = config["Kafka:Topics:ChatEvents"];            

        }

        public Task Publish<TPayload>(Event<TPayload> @event, CancellationToken ct = default)
        {
            var topic = ResolveTopic(@event);
            var key = ResolveKey(@event);

            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(@event, jsonOptions)
            };

            try
            {
                producer.Produce(topic, message, report =>
                {
                    using (logger.Notifiacation(@event))
                    {
                        if (report.Error.IsError)
                        {
                            logger.LogError(
                                "Kafka delivery failed. Code={Code}, Reason={Reason}",
                                report.Error.Code,
                                report.Error.Reason);
                        }
                        else
                        {
                            logger.LogInformation(
                                "Kafka event published to {Topic}, partition={Partition}, offset={Offset}",
                                report.Topic,
                                report.Partition,
                                report.Offset);
                        }
                    }
                });
            }
            catch (ProduceException<string, string> ex)
            {
                using (logger.Notifiacation(@event))
                {
                    logger.LogWarning(
                        "Kafka enqueue failed. Code={Code}, Reason={Reason}",
                        ex.Error.Code,
                        ex.Error.Reason);
                }
            }
            catch (Exception ex)
            {
                using (logger.Notifiacation(@event))
                {
                    logger.LogError(ex, "Kafka unexpected error (ignored)");
                }
            }

            return Task.CompletedTask;
        }

        private string ResolveTopic<TPayload>(Event<TPayload> @event)
        {
            return @event.Type switch
            {
                EventTypes.ChatCreated => chatEventsTopic,
                EventTypes.ChatMessageChanged => chatEventsTopic,

                _ => throw new InvalidOperationException(
                    $"No topic mapping defined for event type '{@event.Type}'")
            };
        }

        private static string ResolveKey<TPayload>(Event<TPayload> @event)
        {
            //if (@event.Payload is ChatMessagePayload chatPayload)
            //    return chatPayload.Chat.Id.ToString();

            return @event.Id.ToString();
        }
    }    
}

