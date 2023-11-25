using IntegrationGatewayService.Models;
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
        Task ManipulateFileAsync<T>(RelayedHttpListenerContext context,T inputRequest);
        // Add other methods as needed
    }
}
