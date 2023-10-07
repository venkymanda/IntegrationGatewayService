using IntegrationGatewayService.Utilities;
using Microsoft.Azure.Relay;
using SampleWorkerApp.Helper;
using SampleWorkerApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IntegrationGatewayService.Services
{
    public class AzureRelayServiceHandler : IAzureRelayServiceHandler
    {
        private readonly ILogger<AzureRelayService> _logger;
        private readonly IAzureRelayServiceHelper _helper;
        private readonly FileManipulatorTypeManager _manager;


        public void HandleRequestBasedonType(RelayedHttpListenerContext context)
        {
            //Add Logic to Get Type from Context and change it Later
            var filemanipulator=_manager.GetFileManipulator(FileManipulatorTypeManager.FileManipulatorType.FileUtils);
            filemanipulator.ManipulateFile(context);
           
        }
    }
}
