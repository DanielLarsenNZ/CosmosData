using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace CosmosData.Tests
{
    [TestClass]
    public class UserPermissions
    {
        string _userWithReadPermissionToken;

        [TestInitialize]
        public async Task CreateUserWithReadPermission()
        {
            // based on https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/UserManagement/UserManagementProgram.cs

            // load configuration from appsettings.json file
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            using (var client = new CosmosClient(config["CosmosData_ConnectionString"]))
            {
                var database = client.GetDatabase(config["CosmosData_DatabaseId"]);
                var container = client.GetContainer(config["CosmosData_DatabaseId"], config["CosmosData_Item_Container"]);

                // Create a user
                User user1 = await database.UpsertUserAsync("readonly_user_test1");

                // Create read permissions on the container
                PermissionProperties readPermission = new PermissionProperties(
                    id: "Read",
                    permissionMode: PermissionMode.Read,
                    container: container);

                // Permissions token will be regenerated here with an expiry of 1 hour
                PermissionProperties permission = await user1.UpsertPermissionAsync(readPermission);

                // Save the permissions in a string variable
                _userWithReadPermissionToken = permission.Token;
            }
        }

        [TestMethod]
        public async Task ConnectWithUserWithReadPermission()
        {
            // load configuration from appsettings.json file
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // initialise Application Insights telemetry client
            var insights = InsightsHelper.InitializeTelemetryClient(config["APPINSIGHTS_INSTRUMENTATIONKEY"]);

            // use the Resource token instead of a master access token
            using (var client = new CosmosClient(config["CosmosData_AccountEndpoint"], _userWithReadPermissionToken))
            {
                // new instance of Data helper
                ICosmosData<Item> data = new CosmosData<Item>(
                    config,
                    insights,
                    client);

                // Get all items
                var getAllItems = await data.GetAll();
            }
        }
    }
}
