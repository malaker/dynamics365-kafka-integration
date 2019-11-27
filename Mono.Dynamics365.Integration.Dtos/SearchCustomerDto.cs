using System;
namespace Mono.Dynamics365.Integration.Dtos
{
    public class SearchCustomerDto
    {
        public Guid CorrelationId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerNumber { get; set; }

        public string CountryIsoCode { get; set; }
    }
}
