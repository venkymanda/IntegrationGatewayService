using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Models
{
    public interface IInputRequest
    {
       
        RequestType? RequestType { get; set; }
        string Source { get; set; }
        string Destination { get; set; }
        string Direction { get; set; }
        string FlowName { get; set; }
        string Data { get; set; }
        string TransactionId { get; set; }
    }
    public enum RequestType
    {
        UploadFile,
        DownloadFile,
        UploadFTPFile,
        DownloadFTPFile,
        SOAPRequest
        // Add more request types as needed
    }
}
