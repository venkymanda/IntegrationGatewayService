using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IntegrationGatewayService.Utilities.FileManipulatorTypeManager;

namespace IntegrationGatewayService.Utilities
{
    public interface IFileManipulatorTypeManager
    {
        IFileManipulator GetFileManipulator(FileManipulatorType type);
    }
}
