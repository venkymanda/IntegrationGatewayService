using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Models
{
    public class FileUploadRequestHeadersDTO:IRequestHeaders
    {
        public string BlobName { get; set; }
        public int ChunkSequence { get; set; }
        public int ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public long TotalSize { get; set; }
        public string TransactionId { get; set; }
        public string InputRequest { get; set; }
        public string RequestType { get; set; }
    }

}
