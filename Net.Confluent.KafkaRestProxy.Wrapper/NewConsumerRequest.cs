using System;
using Newtonsoft.Json;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class NewConsumerRequest
    {
        public string Name { get; set; } = "my_consumer" + Guid.NewGuid();

        public string Format { get; set; } = "json";

        [JsonProperty("auto.offset.reset")]
        public string AutoOffsetReset { get; set; } = "earliest"; //"latest";

        [JsonProperty("auto.commit.enable")]
        public bool AuttoOffsetCommit { get; set; } = false;
    }
}
