using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using System;
using System.IO;
using System.Net;
using IntegrationGatewayService.Models;

namespace SampleWorkerApp.Helper
{
   
        public interface IAzureRelayServiceHelper
    {

        public TokenProvider GetTokenProvider(string keyName = null, string key = null);


        public  string GetTokenAuthString();


        public HybridConnectionListener GetListener();

        public void WriteJsonToContextResponse(RelayedHttpListenerContext context, string json);

        public string ReadContextRequest(RelayedHttpListenerContext context);

        public long GetChunkStart(RelayedHttpListenerContext context);

        public long GetTotalFileSize(RelayedHttpListenerContext context);

        public Guid GetFileId(RelayedHttpListenerContext context);

        public long GetChunkSequence(RelayedHttpListenerContext context);

        public Task WriteToContextResponse(RelayedHttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK);


        public byte[] Decompress(byte[] compressedData);

        (IRequestHeaders, IInputRequest) ExtractRequestFromContext(RelayedHttpListenerContext relayedHttpListenerContext);
    }
}


