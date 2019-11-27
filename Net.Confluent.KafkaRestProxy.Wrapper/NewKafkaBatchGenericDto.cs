using System.Collections.Generic;
using Newtonsoft.Json;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{

    public class NewKafkaBatchGenericDto<T>
    {

        [JsonProperty("records")]
        public List<NewKafkaMessageGeneric<T>> Records { get; set; }

    }

}
