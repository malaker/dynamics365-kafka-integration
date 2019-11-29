using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Mono.Dynamics365.Integration.Dtos;
using Net.Confluent.KafkaRestProxy.Wrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Net.Dynamics365.Kafka.Workflows
{
    public class ConsumerCreatingActivty : CodeActivity
    {
        [Output("Kafka Consumer Uri")]
        public OutArgument<string> ConsumerUri { get; set; }

        protected override void Execute(CodeActivityContext context)
        {

            ITracingService tracer = context.GetExtension<ITracingService>();

            IWorkflowContext econtext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(null);

            tracer.Trace("Getting Kafka Proxy Uri From Config");
            QueryExpression query = new QueryExpression("new_configuration");
            query.ColumnSet = new ColumnSet("new_key", "new_value");
            query.Criteria.AddCondition(new ConditionExpression("new_key", ConditionOperator.Equal, "CL_KAFKA_REST_PROXY"));
            var result = service.RetrieveMultiple(query);
            string value = "";
            if (result.Entities.Any())
            {
                value = (string)result.Entities[0]["new_value"];
            }
            tracer.Trace(string.Format("Retrieved Kafka Proxy Uri From Config {0}", value));

            var consumer = new ConsumerFactory(new ConsumerFactory.ConsumerFactoryOptions() { KafkaProxyUri = value }).Create(
                new ConsumerGroupingRequest() { ConsumerGroup = Guid.NewGuid().ToString(), NewConsumer = new NewConsumerRequest() { } });

            var conusmerResult = consumer.GetAwaiter().GetResult();

            tracer.Trace("Setting:" + conusmerResult.ConsumerBaseUri);
            UriBuilder u0 = new UriBuilder(value);
            UriBuilder u1 = new UriBuilder(conusmerResult.ConsumerBaseUri);
            u1.Port = -1;
            u1.Host = u0.Host;
            var uriToUse = u1.Uri.ToString();

            tracer.Trace(string.Format(" Kafka Proxy Uri To use  {0}", uriToUse));
            ConsumerUri.Set(context, uriToUse);
        }
    }


    public class SubscribeConsumerToTopic : CodeActivity
    {

        [Input("Kafka Consumer Uri")]
        public InArgument<string> ConsumerUriInput { get; set; }

        [Input("Kafka Consumer Topic")]
        public InArgument<string> ConsumerTopic { get; set; }

        [Output("Kafka Consumer Uri")]
        public OutArgument<string> ConsumerUri { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            ITracingService tracer = context.GetExtension<ITracingService>();

            tracer.Trace("Creating consumer");
            UriBuilder uriB = new UriBuilder(ConsumerUriInput.Get(context));
            tracer.Trace(ConsumerUriInput.Get(context));
            tracer.Trace(uriB.Uri.ToString());
            var cw = new ConsumerWrapper<dynamic, SearchCustomerResultDto>(new ConsumerWrapper<dynamic, SearchCustomerResultDto>.ConsumerWrapperOptions()
            {
                ConsumerUri = uriB.Uri.ToString(),
            });
            tracer.Trace(string.Format("Subscribing consumer to topic {0} with uri {1}", ConsumerTopic.Get(context), uriB.Uri.ToString()));

            var result = cw.Subscribe(new List<string>() { ConsumerTopic.Get(context) });

            var resultAwaited = result.GetAwaiter().GetResult();

            tracer.Trace(string.Format("Consumer uri {0} Status code of subscription {1}", uriB.Uri.ToString(), resultAwaited.StatusCode));


            this.ConsumerUri.Set(context, this.ConsumerUri.Get(context));

        }
    }

    public class ConsumeSearchCustomerResultActivity : CodeActivity
    {
        [Input("Customer Search Query")]
        [ReferenceTarget("new_customersearch")]
        public InArgument<EntityReference> CustomerSearchQuery { get; set; }

        [Input("Kafka Consumer Uri")]
        public InArgument<string> ConsumerUriInput { get; set; }

        [Output("Kafka Consumer Uri")]
        public OutArgument<string> ConsumerUri { get; set; }

        private static readonly HttpClient httpClient = new HttpClient();

        protected override void Execute(CodeActivityContext context)
        {
            JsonSerializationException ex;
            ITracingService tracer = context.GetExtension<ITracingService>();
            IWorkflowContext econtext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(null);

            try
            {


                UriBuilder uriB = new UriBuilder(ConsumerUriInput.Get(context));
                tracer.Trace("Use" + uriB.Uri.ToString());

                var cw = new ConsumerWrapper<string, SearchCustomerResultDto>(new ConsumerWrapper<string, SearchCustomerResultDto>.ConsumerWrapperOptions()
                {
                    ConsumerUri = uriB.Uri.ToString()
                }); ;

                // var result = cw.Consume();


                var contractResolver = new CamelCasePropertyNamesContractResolver();

                var jsonSerializerOptions = new JsonSerializerSettings
                {

                    ContractResolver = contractResolver,
                    Formatting = Formatting.Indented
                };


                var uriToUse = uriB.Uri.ToString();
                //consume
                var consumeRequest = new HttpRequestMessage(HttpMethod.Get, $"{uriToUse}/records");

                consumeRequest.Headers.Add("Accept", "application/vnd.kafka.json.v2+json");

                var response =  httpClient.SendAsync(consumeRequest).GetAwaiter().GetResult();
                var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                SearchCustomerResultDto dtoRes=null;
                KafkaRestProxyBatchResponse<dynamic, SearchCustomerResultDto> toUse = null;
                tracer.Trace(text);
                if (text.Contains(this.CustomerSearchQuery.Get(context).Id.ToString()))
                {
                    tracer.Trace("id in message");
                }

                if (!string.IsNullOrEmpty(text))
                {
                    dtoRes = new SearchCustomerResultDto()
                    {
                        CorrelationId = this.CustomerSearchQuery.Get(context).Id,
                        Matches = new List<SearchCustomerSingleResult>() { new SearchCustomerSingleResult() {
                            CountryIsoCode="FI",CustomerName="Test",CustomerNumber="Test"+Guid.NewGuid().ToString(),
                        ExternalSystemId=Guid.NewGuid().ToString(),ExternalSystemName="CoreCRM"} }
                    };

                }

                try
                {
                    tracer.Trace("try 1 single");
                    var resultJson = JsonConvert.DeserializeObject<KafkaRestProxyResponse<dynamic, SearchCustomerResultDto>>(text, jsonSerializerOptions);
                    tracer.Trace("try 1 single success");
                }
                catch(Exception ex2)
                {
                    tracer.Trace(ex2.Message);
                }

                try
                {
                    tracer.Trace("try 1 batch");
                    var resultJson = JsonConvert.DeserializeObject<List<KafkaRestProxyResponse<dynamic, SearchCustomerResultDto>>>(text, jsonSerializerOptions);
                    
                    toUse = new KafkaRestProxyBatchResponse<dynamic, SearchCustomerResultDto>() { Messages = resultJson };
                
                }catch(Exception exc)
                {
                    tracer.Trace(exc.Message);

                    toUse = new KafkaRestProxyBatchResponse<dynamic, SearchCustomerResultDto>()
                    {
                        Messages =
                   new List<KafkaRestProxyResponse<dynamic, SearchCustomerResultDto>>() {
                        new KafkaRestProxyResponse<dynamic, SearchCustomerResultDto>(){
                    Offset=1,Partition=0,Topic="ResponseTestTopic",Value=dtoRes}
               }
                    };

                    tracer.Trace("Use hardcoded");
                }
               

                var recordsResult = toUse;

                Entity customerSearch = null;
                var customerSearchId = this.CustomerSearchQuery.Get(context).Id;

                long maxOffset = -2;
                int partition = -2;
                string topic = "";

                recordsResult.Messages.Where(e => e.Value.CorrelationId == customerSearchId).ToList().ForEach(e =>
                  {

                      if (customerSearchId == e.Value.CorrelationId)
                      {
                          tracer.Trace(string.Format("Message matched by corellation id {0}", customerSearchId));

                          if (customerSearch == null)
                          {
                              customerSearch = service.Retrieve("new_customersearch", customerSearchId, new ColumnSet(true));
                          }

                          if (maxOffset < e.Offset)
                          {
                              maxOffset = e.Offset;
                              partition = e.Partition;
                              topic = e.Topic;
                          }

                          if (customerSearch != null)
                          {
                              e.Value.Matches.ForEach(m =>
                              {
                                  Entity customerSearchResult = new Entity("new_customersearchresult");
                                  customerSearchResult["new_customersearchresultid"] = this.CustomerSearchQuery.Get(context);
                                  customerSearchResult["new_customername"] = m.CustomerName;
                                  customerSearchResult["new_customernumber"] = m.CustomerNumber;
                                  customerSearchResult["new_countryisocode"] = m.CountryIsoCode;
                                  customerSearchResult["new_externalsystemid"] = m.ExternalSystemId;
                                  customerSearchResult["new_externalsystemname"] = m.ExternalSystemName;
                                  tracer.Trace("Creating result");
                                  service.Create(customerSearchResult);
                                  tracer.Trace("Single result created");
                              });

                              customerSearch.Attributes["new_searchstatus"] = new OptionSetValue(100000003);

                              //tracer.Trace("Commiting offset");
                              //var commitTask = cw.Commit(new CommitOffsetsRequest() { Offsets = new List<ConsumerOffset>() { new ConsumerOffset() { Offset = maxOffset, Partition = partition, Topic = topic } } });
                              //var commitResult = commitTask.GetAwaiter().GetResult();
                              //tracer.Trace("Disposing consumer");
                              cw.Dispose().GetAwaiter().GetResult();
                          }
                      }
                  });
            }catch(InvalidPluginExecutionException excc)
            {
                tracer.Trace("Error :"+excc.Message +excc.StackTrace);
                var query = service.Retrieve("new_customersearch", this.CustomerSearchQuery.Get(context).Id, new ColumnSet(true));
                query.Attributes["new_searchstatus"] = new OptionSetValue(1);
                query.Attributes["new_lasterrormessage"] = excc.Message;
                query.Attributes["new_errorscount"] = ((int)query.Attributes["new_errorcount"] + 1);
            }
        }
    }
}
