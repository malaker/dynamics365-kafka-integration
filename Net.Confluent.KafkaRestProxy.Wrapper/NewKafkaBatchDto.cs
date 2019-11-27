using System.Collections.Generic;
using Newtonsoft.Json;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{

    public class NewKafkaBatchDto
    {

        [JsonProperty("records")]
        public List<NewKafkaMessage> Records { get; set; }
    }

    public class NewKafkaMessage
    {
        [JsonProperty("value")]
        public dynamic Value { get; set; }
    }

}
