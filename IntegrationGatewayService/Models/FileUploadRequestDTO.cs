using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Models
{
    public class FileUploadRequestDTO: IInputRequest
    {


        public string? BlobContainerName { get; set; }
        public string? BlobName { get; set; }
        public RequestType? RequestType { get; set; }
        public string? Source { get; set; }
        public string? Destination { get; set; }
        public string? Direction { get; set; }
        public string? FlowName { get; set; }
        public string? Data { get; set; }
       
        public string TransactionId { get; set; }



    }
    
}
