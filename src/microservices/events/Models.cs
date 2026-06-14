using System.Text.Json.Serialization;

namespace EventsService;

public static class KafkaTopics
{
    public const string MovieEvents = "movie-events";
    public const string UserEvents = "user-events";
    public const string PaymentEvents = "payment-events";
}

public class MovieEvent
{
    [JsonPropertyName("movie_id")]
    public int MovieId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("genres")]
    public List<string>? Genres { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class UserEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
}

public class PaymentEvent
{
    [JsonPropertyName("payment_id")]
    public int PaymentId { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("method_type")]
    public string? MethodType { get; set; }
}

public class Event
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("payload")]
    public object Payload { get; set; } = new();
}

public class EventResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("partition")]
    public int Partition { get; set; }

    [JsonPropertyName("offset")]
    public long Offset { get; set; }

    [JsonPropertyName("event")]
    public Event Event { get; set; } = new();
}
