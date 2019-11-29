using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class ConsumerWrapper<K, V>
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private ConsumerWrapperOptions options;

        public class ConsumerWrapperOptions
        {

            public string UriToKafkaProxy { get; set; }

            public string ConsumerGroup { get; set; }

            public string ConsumerUri { get; set; }

            public string ContentType { get; set; } = "application/vnd.kafka.json.v2+json";

            public bool OverrideKafkaBaseUri { get; set; } = false;

            public bool shouldBeDisposeAfterConsumeOperation { get; set; } = false;
        }

        public ConsumerWrapper(ConsumerWrapperOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<HttpResponseMessage> Dispose()
        {
            var uriToUse = this.options.OverrideKafkaBaseUri ? options.ConsumerUri.Replace("kafka-rest", "localhost") : options.ConsumerUri;

            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"{uriToUse}");
            deleteRequest.Headers.Add("Accept", "application/vnd.kafka.v2+json");
            var result = await httpClient.SendAsync(deleteRequest);
            return result;
        }

        public async Task<HttpResponseMessage> Subscribe(IEnumerable<string> topics)
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
            //subcribe

            var uriToUse = this.options.OverrideKafkaBaseUri ? options.ConsumerUri.Replace("kafka-rest", "localhost") : options.ConsumerUri;

            if (string.IsNullOrEmpty(uriToUse))
            {
                throw new ArgumentException("Null or empty uri");
            }



            var requestSubscribe = new HttpRequestMessage(HttpMethod.Post, $"{uriToUse}/subscription");


            var subscribeDto = new { Topics = topics };

            string jsonNewSubscription = JsonConvert.SerializeObject(subscribeDto, subscribeDto.GetType(), jsonSerializerOptions);

            requestSubscribe.Content = new StringContent(jsonNewSubscription, System.Text.Encoding.UTF8, "application/vnd.kafka.v2+json");

            var response = await httpClient.SendAsync(requestSubscribe).ConfigureAwait(false);

            return response;

        }

        public async Task<KafkaRestProxyBatchResponse<K, V>> Consume()
        {

            var contractResolver = new CamelCasePropertyNamesContractResolver();

            var jsonSerializerOptions = new JsonSerializerSettings
            {

                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            };


            var uriToUse = this.options.OverrideKafkaBaseUri ? options.ConsumerUri.Replace("kafka-rest", "localhost") : options.ConsumerUri;
            //consume
            var consumeRequest = new HttpRequestMessage(HttpMethod.Get, $"{uriToUse}/records");

            consumeRequest.Headers.Add("Accept", options.ContentType);

            var response = await httpClient.SendAsync(consumeRequest).ConfigureAwait(false);
            var text=await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<List<KafkaRestProxyResponse<K, V>>>(text, jsonSerializerOptions);

            return new KafkaRestProxyBatchResponse<K, V>() { Messages = result };
        }


        public async Task<HttpResponseMessage> Commit(CommitOffsetsRequest request)
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


            var uriToUse = this.options.OverrideKafkaBaseUri ? options.ConsumerUri.Replace("kafka-rest", "localhost") : options.ConsumerUri;
            //consume
            var commitRequest = new HttpRequestMessage(HttpMethod.Post, $"{uriToUse}/offsets");

            commitRequest.Content = new StringContent(JsonConvert.SerializeObject(request, request.GetType(), jsonSerializerOptions), System.Text.Encoding.UTF8, options.ContentType);

            //commitRequest.Headers.Add("Accept", options.ContentType);

            var response = await httpClient.SendAsync(commitRequest).ConfigureAwait(false);

            return response;
        }
    }
}
