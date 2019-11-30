using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Mono.Dynamics365.Integration.Dtos;
using Net.Confluent.KafkaRestProxy.Wrapper;

namespace Net.Dynamics365.Kafka.Workflows
{
    public class ProduceSearchQueryActivity : CodeActivity
    {
        [Input("Customer Search Query")]
        [ReferenceTarget("new_customersearch")]
        public InArgument<EntityReference> CustomerSearchQuery { get; set; }

        [Input("TopicName")]
        public InArgument<string> TopicName { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            
            ITracingService tracingService = context.GetExtension<ITracingService>();
            try
            {
                IWorkflowContext econtext = context.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
                IOrganizationService service = serviceFactory.CreateOrganizationService(null);

                tracingService.Trace("Getting Kafka Proxy Uri From Config");
                QueryExpression query = new QueryExpression("new_configuration");
                query.ColumnSet = new ColumnSet("new_key", "new_value");
                query.Criteria.AddCondition(new ConditionExpression("new_key", ConditionOperator.Equal, "CL_KAFKA_REST_PROXY"));
                var result = service.RetrieveMultiple(query);
                string value = "";
                if (result.Entities.Any())
                {
                    value = (string)result.Entities[0]["new_value"];
                }
                tracingService.Trace(string.Format("Retrieved Kafka Proxy Uri From Config {0}", value));



                tracingService.Trace("Creating producer...");
                tracingService.Trace(value);

                DefaultHttpProducer producer = new DefaultHttpProducer(
                    new DefaultHttpProducer.ProducerOptions() { KafkaProxyUri = value });

                tracingService.Trace("Producer created");

                tracingService.Trace("Creating search dto");

                SearchCustomerDto dto = new SearchCustomerDto();
                var entity = service.Retrieve("new_customersearch", CustomerSearchQuery.Get(context).Id, new ColumnSet(true));
                tracingService.Trace("Search dto created");

                if (entity.LogicalName == "new_customersearch")
                {
                    dto.CorrelationId = entity.Id;
                    dto.CustomerName = entity.Attributes["new_customername"].ToString();
                    dto.CustomerNumber = entity.Attributes["new_customernumber"].ToString();
                    dto.CountryIsoCode = entity.Attributes["new_countryisocode"].ToString();
                }

                tracingService.Trace("Publishing search dto to Kafka");
                tracingService.Trace(string.Format("Topic Name {0}", TopicName.Get(context)));
                tracingService.Trace(string.Format("Uri to kafka {0}", value));
                var resultHtp = producer.Produce<SearchCustomerDto>(dto, TopicName.Get(context)).GetAwaiter().GetResult();

                if (!resultHtp.IsSuccessStatusCode)
                {
                    tracingService.Trace(string.Format("Error during publishing:StatusCode {0}, Reason {1}", resultHtp.StatusCode, resultHtp.ReasonPhrase));
                }
                else
                {
                    tracingService.Trace("Search dto published");
                    entity.Attributes["new_published"] = "true";
                    service.Update(entity);
                }
            }catch(Exception ex)
            {
                tracingService.Trace(ex.Message + ex.StackTrace);
                throw;
            }
        }
    }
}

