using Microsoft.Azure.Relay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IntegrationGatewayService.Services
{
    public interface IAzureRelayServiceHandler
    {
        public void HandleRequestBasedonType(RelayedHttpListenerContext context);
    }
}
