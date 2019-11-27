namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class ConsumerGroupingRequest
    {
        public string ConsumerGroup { get; set; }

        public NewConsumerRequest NewConsumer { get; set; }
    }
}
