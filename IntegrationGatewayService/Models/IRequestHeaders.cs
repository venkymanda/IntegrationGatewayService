using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Models
{
    public interface IRequestHeaders
    {
        string InputRequest { get; set; }
        string RequestType { get; set; }
    }
}
