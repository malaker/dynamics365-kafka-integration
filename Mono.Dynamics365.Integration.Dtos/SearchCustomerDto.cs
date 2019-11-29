using System;
using System.Collections.Generic;

namespace Mono.Dynamics365.Integration.Dtos
{
    public class SearchCustomerDto
    {
        public Guid CorrelationId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerNumber { get; set; }

        public string CountryIsoCode { get; set; }
    }

    public class SearchCustomerSingleResult
    {
        public string CustomerName { get; set; }

        public string CustomerNumber { get; set; }

        public string CountryIsoCode { get; set; }

        public string ExternalSystemId { get; set; }

        public string ExternalSystemName { get; set; }
    }

    public class SearchCustomerResultDto
    {
        public Guid CorrelationId { get; set; }

        public List<SearchCustomerSingleResult> Matches { get; set; }
        

        
    }

}
