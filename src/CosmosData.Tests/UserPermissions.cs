using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CosmosData.Tests
{
    [TestClass]
    public class UserPermissions : CosmosDataTest
    {
        string _userWithReadPermissionToken;

        [TestInitialize]
        public async Task CreateUserWithReadPermission()
        {
            // based on https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/UserManagement/UserManagementProgram.cs

            using (var client = new CosmosClient(_config["CosmosData_ConnectionString"]))
            {
                var database = client.GetDatabase(_config[CosmosDataDatabaseIdKey]);
                var container = client.GetContainer(_config[CosmosDataDatabaseIdKey], _config[CosmosDataContainerIdKey]);

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
            // use the Resource token instead of a master access token
            using (var client = new CosmosClient(_config["CosmosData_AccountEndpoint"], _userWithReadPermissionToken))
            {
                // new instance of Data helper
                var data = new CosmosData<Item>(_insights, client, _config[CosmosDataDatabaseIdKey], _config[CosmosDataContainerIdKey]);

                // Get all items
                var getAllItems = await data.GetAll();
            }
        }
    }
}
