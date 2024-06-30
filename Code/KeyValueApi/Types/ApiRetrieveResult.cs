public record ApiRetrieveResult
{
    public required string ValueB64 { get; init; }
    public Dictionary<string, string>? KafkaHeaders { get; init; }
    public required string CorrelationId { get; init; }
    public required string KafkaEventMetadata { get; init; }
}

