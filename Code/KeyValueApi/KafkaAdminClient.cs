using Confluent.Kafka;
using Confluent.Kafka.Admin;

public class KafkaAdminClient
{
    private readonly ILogger<KafkaAdminClient> _logger;

    public KafkaAdminClient(ILogger<KafkaAdminClient> logger)
    {
        _logger = logger;
        logger.LogInformation($"{nameof(KafkaAdminClient)} initialized");
    }

    public async Task<bool> TryCreateTopics()
    {
        var adminClientConfig = KafkaAdminClientEnvBinder.GetAdminClientConfig();
        var topic = Environment.GetEnvironmentVariable(KV_API_KAFKA_KEY_VALUE_TOPIC);

        using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
        try
        {
            // List<DescribeConfigsResult> preExistingConfigs = await adminClient.DescribeConfigsAsync(
            //     [
            //         new ConfigResource
            //         {
            //             Name = topic,
            //             Type = ResourceType.Topic,
            //         }
            //     ]
            // );
            // if(preExistingConfigs.Any())
            // {
            //     return false;
            // }
            _logger.LogInformation($"Asking admin client to create topic {topic} in case it doesn't exist");
            await adminClient.CreateTopicsAsync(new TopicSpecification[] {
                    new TopicSpecification
                    {
                         Name = topic,
                         ReplicationFactor = -1,
                         NumPartitions = 1,
                         Configs = new Dictionary<string, string>
                         {
                            { "cleanup.policy", "compact" },
                            { "retention.bytes", "-1" },
                            { "retention.ms", "-1" },
                         }
                    }
                });
            _logger.LogInformation($"Admin client done creating topic {topic}");
            return true;
        }
        catch (Exception e)
        {
            if (e is Confluent.Kafka.Admin.CreateTopicsException && e.Message.Contains($"Topic '{topic}' already exists."))
            {
                // Doing it this way with exception to check is kind of bad, but for a 1-off "check this during startup" it's not really worth it to start the "query cluster for info about everything including all topics" dance.
                // Still, don't do this at home kids.
                _logger.LogInformation($"Admin client did not create topic {topic} because it already exists");
                return false;
            }
            _logger.LogError(e, $"An error occurred creating topic");
        }

        return false;
    }

    public async Task<List<TopicPartition>> GetTopicPartitions(KafkaTopic topic)
    {
        var adminClientConfig = KafkaAdminClientEnvBinder.GetAdminClientConfig();
        using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
        try
        {
            var description = await adminClient.DescribeTopicsAsync(TopicCollection.OfTopicNames([topic.Value]));
            List<TopicPartition> topicPartitions = description.TopicDescriptions
                .FirstOrDefault(tDescription => tDescription.Name == topic.Value)
                ?.Partitions
                .Select(tpInfo => new TopicPartition(topic.Value, tpInfo.Partition))
                .ToList() ?? [];
            return topicPartitions;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"An error occurred when retrieving list of partitions on topic");
        }
        return [];
    }

}
