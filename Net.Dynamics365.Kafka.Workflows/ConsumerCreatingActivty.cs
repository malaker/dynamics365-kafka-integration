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
        [Input("Auto offset reset")]
        public InArgument<string> AutoOffsetReset { get; set; }

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
                new ConsumerGroupingRequest() { ConsumerGroup = Guid.NewGuid().ToString(), NewConsumer = new NewConsumerRequest() {AutoOffsetReset= string.IsNullOrEmpty(this.AutoOffsetReset.Get(context)) ? "latest":this.AutoOffsetReset.Get(context) } });

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
                KafkaRestProxyBatchResponse<string, SearchCustomerResultDto> toUse = null;
                try
                {
                    var result = cw.Consume().GetAwaiter().GetResult();
                    toUse = result;

                }catch(Exception ex)
                {
                    tracer.Trace(ex.Message + ex.StackTrace);
                }

                var recordsResult = toUse ?? new KafkaRestProxyBatchResponse<string, SearchCustomerResultDto>() { Messages = new List<KafkaRestProxyResponse<string, SearchCustomerResultDto>>()};

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
                                  customerSearchResult["new_customersearchresultrecordid"] = this.CustomerSearchQuery.Get(context);
                                  customerSearchResult["new_name"] = m.CustomerName;
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
                              service.Update(customerSearch);
                              tracer.Trace("Commiting offset");
                              var commitTask = cw.Commit(new CommitOffsetsRequest() { Offsets = new List<ConsumerOffset>() { new ConsumerOffset() { Offset = maxOffset, Partition = partition, Topic = topic } } });
                              var commitResult = commitTask.GetAwaiter().GetResult();
                              tracer.Trace("Disposing consumer");
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
