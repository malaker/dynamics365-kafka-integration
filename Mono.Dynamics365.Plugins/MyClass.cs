using System;
using Microsoft.Xrm.Sdk;

namespace Mono.Dynamics365.Plugins
{
    public class TestPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
(ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Hello world");
        }
    }
}
