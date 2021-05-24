using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CosmosData.Tests
{
    /// <summary>
    /// Test helper base class
    /// </summary>
    public abstract class CosmosDataTest
    {
        // load configuration from appsettings.json file
        protected static readonly IConfiguration _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        protected const string CosmosDataConnectionStringKey = "CosmosData_ConnectionString";
        protected const string CosmosDataDatabaseIdKey = "CosmosData_DatabaseId";
        protected const string CosmosDataContainerIdKey = "CosmosData_Item_Container";

        // initialise Application Insights telemetry client
        protected static readonly TelemetryClient _insights = InsightsHelper.InitializeTelemetryClient(_config["APPINSIGHTS_INSTRUMENTATIONKEY"]);

        public CosmosDataTest()
        {
            if (string.IsNullOrEmpty(_config[CosmosDataConnectionStringKey])) throw new MissingConfigurationException(CosmosDataConnectionStringKey);
            if (string.IsNullOrEmpty(_config[CosmosDataDatabaseIdKey])) throw new MissingConfigurationException(CosmosDataDatabaseIdKey);
            if (string.IsNullOrEmpty(_config[CosmosDataContainerIdKey])) throw new MissingConfigurationException(CosmosDataContainerIdKey);
        }
    }
}
