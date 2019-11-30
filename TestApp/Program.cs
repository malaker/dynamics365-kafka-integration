using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Dynamics365.Integration.Dtos;
using Net.Confluent.KafkaRestProxy.Wrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var contractResolver = new CamelCasePropertyNamesContractResolver();
            string host = "kafka.local.com";
            string proxy = "http://kafka.local.com";
            var jsonSerializerOptions = new JsonSerializerSettings
            {

                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            };

            ConsumerFactory consumerFactory = new ConsumerFactory(new ConsumerFactory.ConsumerFactoryOptions()
            {
                KafkaProxyUri = proxy
            });

            var cr=consumerFactory.Create(new ConsumerGroupingRequest() { 
                ConsumerGroup = Guid.NewGuid().ToString(), 
                NewConsumer = new NewConsumerRequest() {AutoOffsetReset="latest" } }).GetAwaiter().GetResult();

            UriBuilder ub = new UriBuilder(cr.ConsumerBaseUri);
            ub.Port = -1;
            ub.Host = host;

            ConsumerWrapper<dynamic, SearchCustomerDto> cw = new ConsumerWrapper<dynamic, SearchCustomerDto>(new ConsumerWrapper<dynamic, SearchCustomerDto>.ConsumerWrapperOptions()
            {
                ConsumerUri = ub.Uri.ToString()
            }) ;

            cw.Subscribe(new List<string>() { "QueryTestTopic" }).GetAwaiter().GetResult();

            DefaultHttpProducer producer = new DefaultHttpProducer(new DefaultHttpProducer.ProducerOptions() { KafkaProxyUri = proxy ,SerializerOptions = jsonSerializerOptions });


            while (true)
            {
                var consumeResult = cw.Consume().GetAwaiter().GetResult();

                if (consumeResult.Messages.Any())
                {
                    consumeResult.Messages.ForEach(m =>
                    {

                        SearchCustomerResultDto dto = new SearchCustomerResultDto()
                        {
                            CorrelationId = m.Value.CorrelationId,
                            Matches = new List<SearchCustomerSingleResult>() { 
                                new SearchCustomerSingleResult() { CountryIsoCode = "FI", CustomerName = Guid.NewGuid().ToString(), CustomerNumber = Guid.NewGuid().ToString(), ExternalSystemId = "ex", ExternalSystemName = "sys" } }

                        };
                        var result = producer.Produce<SearchCustomerResultDto>(dto, "ResponseTestTopic").GetAwaiter().GetResult();
                        cw.Commit(new CommitOffsetsRequest()
                        {
                            Offsets = new List<ConsumerOffset>() { new ConsumerOffset() {
                            Offset=m.Offset,
                            Partition=m.Partition,
                            Topic=m.Topic
                        } }
                        } 
                        ).GetAwaiter().GetResult();
                    });
                }
            }
         
        }
    }
}
