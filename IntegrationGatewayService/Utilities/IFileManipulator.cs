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
        Task ManipulateFileAsync(RelayedHttpListenerContext context,FileUploadRequestDTO inputRequest, FileUploadRequestHeadersDTO requestHeaders);
        // Add other methods as needed
    }
}
