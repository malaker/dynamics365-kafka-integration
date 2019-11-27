using Newtonsoft.Json;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{

    public class NewKafkaMessageGeneric<T>
    {
        [JsonProperty("value")]
        public T Value { get; set; }
    }

}
