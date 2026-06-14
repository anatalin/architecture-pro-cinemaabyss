using Confluent.Kafka;

namespace EventsService;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private readonly string _bootstrapServers;

    public KafkaConsumerHostedService(ILogger<KafkaConsumerHostedService> logger)
    {
        _logger = logger;
        _bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "localhost:9092";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private void Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "events-service-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<Null, string>(config).Build();
        consumer.Subscribe(new[] { KafkaTopics.MovieEvents, KafkaTopics.UserEvents, KafkaTopics.PaymentEvents });

        _logger.LogInformation("Kafka consumer subscribed to topics: {Topics}",
            string.Join(", ", KafkaTopics.MovieEvents, KafkaTopics.UserEvents, KafkaTopics.PaymentEvents));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    _logger.LogInformation(
                        "Consumed event from topic '{Topic}' (partition {Partition}, offset {Offset}): {Value}",
                        result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested
        }
        finally
        {
            consumer.Close();
        }
    }
}
