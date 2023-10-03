# Integration Gateway Service

![GitHub repo size](https://img.shields.io/github/repo-size/venkymanda/IntegrationGatewayService)
![GitHub stars](https://img.shields.io/github/stars/venkymanda/IntegrationGatewayService)
![GitHub forks](https://img.shields.io/github/forks/venkymanda/IntegrationGatewayService)
![GitHub issues](https://img.shields.io/github/issues/venkymanda/IntegrationGatewayService)
![GitHub license](https://img.shields.io/github/license/venkymanda/IntegrationGatewayService)

The Integration Gateway Service is a Windows service designed to handle integrations from on-premises servers using Azure Relay. This repository contains the source code and documentation for the service.

## Table of Contents

- [Introduction](#introduction)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Introduction

Integrating on-premises servers with cloud services can be a challenging task. The Integration Gateway Service simplifies this process by utilizing Azure Relay to securely transmit data between on-premises systems and cloud services.

Key features of the Integration Gateway Service:
- Seamless integration with Azure Relay for secure communication.
- Windows service architecture for reliability and automatic startup.
- Customizable configuration to adapt to various integration scenarios.

## Prerequisites

Before you begin, ensure you have met the following requirements:

- .NET Framework installed on your Windows server.
- Azure account with access to Azure Relay.

## Getting Started

To get started with the Integration Gateway Service, follow these steps:

1. Clone this repository to your local machine:

   ```bash
   git clone https://github.com/venkymanda/IntegrationGatewayService.git
2. Build the project using Visual Studio or the .NET CLI.

3. Configure the service by editing the app.config file with your Azure Relay credentials and integration settings.

4. Install the service by running the following command as an administrator:
sc create IntegrationGatewayService binPath= "C:\Path\To\Your\Service\IntegrationGatewayService.exe"
5. Start the service using the following command:
sc start IntegrationGatewayService

## Configuration

The service's configuration is stored in the app.config file. You can customize settings such as Azure Relay connection strings, integration endpoints, and logging options in this file.

<configuration>
    <!-- Azure Relay Configuration -->
    <appSettings>
        <add key="AzureRelayConnectionString" value="YOUR_AZURE_RELAY_CONNECTION_STRING" />
        <!-- Add more configuration keys as needed -->
    </appSettings>
    <!-- Other Configuration Settings -->
</configuration>


## Usage

Once the service is up and running, it will handle integrations from your on-premises servers to Azure Relay endpoints. Monitor the service's logs for any issues or integration details.

## Contributing

Contributions are welcome! If you have any improvements or feature requests, please submit an issue or a pull request. We appreciate your help in making this service better.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.