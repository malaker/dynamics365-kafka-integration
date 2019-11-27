using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class ConsumerFactory
    {
        private static readonly HttpClient client = new HttpClient();
        private ConsumerFactoryOptions options;

        public ConsumerFactory(ConsumerFactoryOptions options)
        {
            this.options = options;
        }

        public class ConsumerFactoryOptions
        {
            public string KafkaProxyUri { get; set; }
        }

        public async Task<NewConsumerResponse> Create(ConsumerGroupingRequest newConsumerReq)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var jsonSerializerOptions = new JsonSerializerSettings
            {

                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            };

            //create consumer
            var request = new HttpRequestMessage(HttpMethod.Post, $"{options.KafkaProxyUri}/consumers/{newConsumerReq.ConsumerGroup}/");

            request.Headers.Add("Accept", "application/vnd.kafka.v2+json");

            string jsonNewConsumer = JsonConvert.SerializeObject(newConsumerReq.NewConsumer, newConsumerReq.NewConsumer.GetType(), jsonSerializerOptions);

            request.Content = new StringContent(jsonNewConsumer, System.Text.Encoding.UTF8, "application/vnd.kafka.v2+json");


            var response = await client.SendAsync(request).ConfigureAwait(false);
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            NewConsumerResponse consumerData = JsonConvert.DeserializeObject<NewConsumerResponse>(stringResponse, jsonSerializerOptions);

            return consumerData;
        }
    }
}
