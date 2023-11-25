using IntegrationGatewayService.Models;
using IntegrationGatewayService.Utilities;
using Microsoft.Azure.Relay;
using SampleWorkerApp.Helper;
using SampleWorkerApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IntegrationGatewayService.Services
{
    public class AzureRelayServiceHandler : IAzureRelayServiceHandler
    {
        private readonly ILogger<AzureRelayServiceHandler> _logger;
        private readonly IAzureRelayServiceHelper _helper;
        private readonly IFileManipulatorTypeManager _manager;

        public AzureRelayServiceHandler(ILogger<AzureRelayServiceHandler> logger, IAzureRelayServiceHelper helper, IFileManipulatorTypeManager manager)
        {
            _logger = logger;
            _helper = helper;
            _manager = manager;
           

        }

        public async Task HandleRequestBasedonTypeAsync(RelayedHttpListenerContext context)
        {
            try
            {
                //Add Logic to Get Type from Context and change it Later
                // Use the helper method to extract request data and its type
                (IRequestHeaders headers, IInputRequest request) = _helper.ExtractRequestFromContext(context);

                if (headers != null && request != null && !string.IsNullOrEmpty(headers.RequestType))
                {
                    // Process the request based on its type


                    switch (headers.RequestType)
                    {
                        case "UploadFile":
                            var fileManipulator = _manager.GetFileManipulator(FileManipulatorTypeManager.FileManipulatorType.FileUtils);
                            await fileManipulator.ManipulateFileAsync(context, (FileUploadRequestDTO)request);
                            break;
                        case "DownloadFile":
                            // Use a different file manipulator for downloads if needed
                            // var downloadFileManipulator = _manager.GetFileManipulator(FileManipulatorTypeManager.FileManipulatorType.DownloadFile);
                            // downloadFileManipulator.ManipulateFile(context, (DownloadRequest)request);
                            break;
                        case "Process":
                            // Use a different file manipulator for processing if needed
                            // var processFileManipulator = _manager.GetFileManipulator(FileManipulatorTypeManager.FileManipulatorType.ProcessFile);
                            // processFileManipulator.ManipulateFile(context, (ProcessRequest)request);
                            break;
                        // Handle other request types as needed
                        default:
                            // Handle unsupported request types
                            break;
                    }
                }
                else
                {
                    // Handle the case where the request data or type is null or missing
                    throw new Exception();
                }
            }
            
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during processing
                // Log the exception or take appropriate error-handling actions
                _logger.LogError(ex.Message, ex);
            }

}
    }
}
