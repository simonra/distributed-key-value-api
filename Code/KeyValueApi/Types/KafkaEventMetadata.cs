public record KafkaEventMetadata
{
    public required string Topic { get; init; }
    public required long Partition { get; init; }
    public required long Offset { get; init; }
    public required string TopicTimestamp { get; init; }
}
