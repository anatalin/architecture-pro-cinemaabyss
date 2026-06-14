using Confluent.Kafka;
using EventsService;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8082";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "localhost:9092";

builder.Services.AddSingleton<IProducer<Null, string>>(_ =>
    new ProducerBuilder<Null, string>(new ProducerConfig { BootstrapServers = kafkaBrokers }).Build());

builder.Services.AddHostedService<KafkaConsumerHostedService>();

var app = builder.Build();

var logger = app.Logger;

app.MapGet("/api/events/health", () => Results.Json(new { status = true }));

app.MapPost("/api/events/movie", async (MovieEvent input, IProducer<Null, string> producer) =>
{
    var evt = new Event
    {
        Id = $"movie-{input.MovieId}-{input.Action}",
        Type = "movie",
        Timestamp = DateTime.UtcNow,
        Payload = input,
    };

    var response = await PublishEventAsync(producer, KafkaTopics.MovieEvents, evt, logger);
    return Results.Json(response, statusCode: StatusCodes.Status201Created);
});

app.MapPost("/api/events/user", async (UserEvent input, IProducer<Null, string> producer) =>
{
    var evt = new Event
    {
        Id = $"user-{input.UserId}-{input.Action}",
        Type = "user",
        Timestamp = input.Timestamp ?? DateTime.UtcNow,
        Payload = input,
    };

    var response = await PublishEventAsync(producer, KafkaTopics.UserEvents, evt, logger);
    return Results.Json(response, statusCode: StatusCodes.Status201Created);
});

app.MapPost("/api/events/payment", async (PaymentEvent input, IProducer<Null, string> producer) =>
{
    var evt = new Event
    {
        Id = $"payment-{input.PaymentId}-{input.Status}",
        Type = "payment",
        Timestamp = input.Timestamp ?? DateTime.UtcNow,
        Payload = input,
    };

    var response = await PublishEventAsync(producer, KafkaTopics.PaymentEvents, evt, logger);
    return Results.Json(response, statusCode: StatusCodes.Status201Created);
});

app.Run();

static async Task<EventResponse> PublishEventAsync(IProducer<Null, string> producer, string topic, Event evt, ILogger logger)
{
    var json = System.Text.Json.JsonSerializer.Serialize(evt);

    var deliveryResult = await producer.ProduceAsync(topic, new Message<Null, string> { Value = json });

    logger.LogInformation("Produced event '{EventId}' to topic '{Topic}' (partition {Partition}, offset {Offset})",
        evt.Id, topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value);

    return new EventResponse
    {
        Status = "success",
        Partition = deliveryResult.Partition.Value,
        Offset = deliveryResult.Offset.Value,
        Event = evt,
    };
}
