using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Utilities
{
    public class FileManipulatorTypeManager:IFileManipulatorTypeManager
    {
        // Enum to define the types of file manipulators
        public enum FileManipulatorType
        {
            FileUtils,
            FtpUtils,
            // Add other types as needed
        }
        private readonly IServiceProvider _serviceProvider;

        public FileManipulatorTypeManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IFileManipulator GetFileManipulator(FileManipulatorType type)
        {
            switch (type)
            {
                case FileManipulatorType.FileUtils:
                    return _serviceProvider.GetRequiredService<FileUtils>();
                // Add other types as needed
                default:
                    throw new ArgumentException("Invalid file manipulator type");
            }
        }
    }
}
