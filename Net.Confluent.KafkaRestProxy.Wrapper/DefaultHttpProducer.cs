using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class DefaultHttpProducer : IHttpKafkaProducer
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private ProducerOptions options;

        public class ProducerOptions
        {
            public JsonSerializerSettings SerializerOptions { get; set; }
            public string KafkaProxyUri { get; set; }
        }

        public DefaultHttpProducer(ProducerOptions options)
        {
            this.options = options;
        }

        public async Task<HttpResponseMessage> Produce<T>(T value, string topic)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var optionsToUse = this.options?.SerializerOptions ?? new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            };

            var batch = new NewKafkaBatchGenericDto<T>() { Records = new List<NewKafkaMessageGeneric<T>>() };

            batch.Records.Add(new NewKafkaMessageGeneric<T>() { Value = value });

            string json = JsonConvert.SerializeObject(batch, batch.GetType(), optionsToUse);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{this.options.KafkaProxyUri}/topics/{topic}");

            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/vnd.kafka.json.v2+json");

            var response = await httpClient.SendAsync(request);

            return response;

        }

        public async Task<HttpResponseMessage> Produce(dynamic value, string topic)
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

            var optionsToUse = this.options?.SerializerOptions ?? jsonSerializerOptions;


            NewKafkaBatchDto batch = new NewKafkaBatchDto() { Records = new List<NewKafkaMessage>() };

            batch.Records.Add(new NewKafkaMessage() { Value = value });

            string json = JsonConvert.SerializeObject(batch, typeof(NewKafkaBatchDto), optionsToUse);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{this.options.KafkaProxyUri}/topics/{topic}");

            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/vnd.kafka.json.v2+json");

            var response = await httpClient.SendAsync(request);

            return response;
        }
    }
}
