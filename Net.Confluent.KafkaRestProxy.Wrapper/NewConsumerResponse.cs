using Newtonsoft.Json;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
   
        public class NewConsumerResponse
        {
            //{"instance_id":"my_consumer_instance","base_uri":"http://localhost:8082/consumers/my_json_consumer/instances/my_consumer_instance"}
            [JsonProperty("instance_id")]
            public string InstanceId { get; set; }
            [JsonProperty("base_uri")]
            public string ConsumerBaseUri { get; set; }
        }
    
}
