using Microsoft.Extensions.Logging;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace EBAY.TRADING.API.CLIENT.Inspectors
{
    internal class SoapLoggerBehaviour : IEndpointBehavior
    {
        private readonly ILogger log;

        public SoapLoggerBehaviour(ILogger log)
        {
            this.log = log;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new SoapLoggerInspector(this.log));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {

        }

        public void Validate(ServiceEndpoint endpoint)
        {

        }
    }
}
