using Microsoft.Azure.Relay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Utilities
{
    public interface IFileManipulator
    {
        void ManipulateFile(RelayedHttpListenerContext context);
        // Add other methods as needed
    }
}
