using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace states.Services.Events.Consumer;

public class GlobalEventConsumerService(
    IConfiguration config,
    IGlobalEventProcessor processor,
    ILogger<GlobalEventConsumerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = config["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");

        var topic = config["Kafka:Topics:GlobalEvents"]
            ?? throw new InvalidOperationException("Kafka:Topics:GlobalEvents not configured");

        var groupId = config["Kafka:ConsumerGroup"] ?? "states-service";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        logger.LogInformation(
            "Kafka consumer started. Topic={Topic}, Group={Group}",
            topic, groupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);

                var eventType = ExtractEventType(result.Message.Value);
                if (eventType is null)
                {
                    logger.LogWarning("Could not extract event type from message, skipping");
                    consumer.Commit(result);
                    continue;
                }

                await processor.Process(eventType, result.Message.Value, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing Kafka message");
                if (result is not null)
                    consumer.Commit(result);
            }
        }

        consumer.Close();
        logger.LogInformation("Kafka consumer stopped");
    }

    private static string? ExtractEventType(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("type", out var typeProp))
                return typeProp.GetString();
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }
}
