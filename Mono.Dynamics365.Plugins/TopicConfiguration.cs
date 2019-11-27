using System;
using Newtonsoft.Json;

namespace Mono.Dynamics365.Plugins
{
    public class TopicConfiguration
    {
        [JsonProperty("topicName")]
        public string TopicName { get; set; }

        [JsonProperty("kafkaProxyUri")]
        public string KafkaProxyUri { get; set; }

        public TopicConfiguration()
        {
        }
    }
}
