using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Mono.Dynamics365.Integration.Dtos;
using Net.Confluent.KafkaRestProxy.Wrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mono.Dynamics365.Plugins
{
    public class SearchCustomerInExternalSystemPlugin : IPlugin
    {
        private JsonSerializerSettings serializeOptions;
        private string conn;
        private TopicConfiguration configurationObject;

        public SearchCustomerInExternalSystemPlugin(string unsecureString, string secureString)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var jsonSerializerOptions = new JsonSerializerSettings
            {

                ContractResolver = contractResolver
            };

            this.serializeOptions = jsonSerializerOptions;
            this.conn = unsecureString;
            this.configurationObject = (Mono.Dynamics365.Plugins.TopicConfiguration)JsonConvert.DeserializeObject(unsecureString, typeof(TopicConfiguration), serializeOptions);
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
(ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Creating producer...");
            tracingService.Trace(this.conn);

            DefaultHttpProducer producer = new DefaultHttpProducer(
                new DefaultHttpProducer.ProducerOptions() { KafkaProxyUri = this.configurationObject.KafkaProxyUri });

            tracingService.Trace("Producer created");

            tracingService.Trace("Creating search dto");

            SearchCustomerDto dto = new SearchCustomerDto();

            tracingService.Trace("Search dto created");

            // Obtain the execution context from the service provider.
            Microsoft.Xrm.Sdk.IPluginExecutionContext context = (Microsoft.Xrm.Sdk.IPluginExecutionContext)
            serviceProvider.GetService(typeof(Microsoft.Xrm.Sdk.IPluginExecutionContext));


            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") &&
            context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];

                if (entity.LogicalName == "new_customersearch")
                {
                    dto.CorrelationId = entity.Id;
                    dto.CustomerName = entity.Attributes["new_customername"].ToString();
                    dto.CustomerNumber = entity.Attributes["new_customernumber"].ToString();
                    dto.CountryIsoCode = entity.Attributes["new_countryisocode"].ToString();
                }

                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);



                tracingService.Trace("Publishing search dto to Kafka");
                tracingService.Trace(string.Format("Topic Name {0}", this.configurationObject.TopicName));
                tracingService.Trace(string.Format("Uri to kafka {0}",this.configurationObject.KafkaProxyUri));
                var result = producer.Produce<SearchCustomerDto>(dto, configurationObject.TopicName).GetAwaiter().GetResult();

                if (!result.IsSuccessStatusCode)
                {
                    tracingService.Trace(string.Format("Error during publishing:StatusCode {0}, Reason {1}",result.StatusCode,result.ReasonPhrase));
                }
                else
                {
                    tracingService.Trace("Search dto published");
                    entity.Attributes["new_published"] = "true";
                    service.Update(entity);
                }
            }
        }
    }
}
