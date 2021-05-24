using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosData.Tests
{
    /// <summary>
    /// An example of extending CosmosData<![CDATA[<T>]]> with a query Filter 
    /// </summary>
    public class ItemData : CosmosData<Item>
    {
        public ItemData(IConfiguration config, TelemetryClient telemetry, CosmosClient cosmos) :
            base(telemetry, cosmos, config["CosmosData_DatabaseId"], config["CosmosData_Item_Container"])
        {
        }

        /// <summary>
        /// Get all items that match the provided Category
        /// </summary>
        /// <param name="category">The Category</param>
        /// <returns>An IEnumerable of T containing the matched items.</returns>
        public async Task<IEnumerable<Item>> GetFilteredByCategory(string category) => await GetWithQuery(
                new QueryDefinition($"SELECT * FROM {_container.Id} c WHERE c.Category = @category")
                    .WithParameter("@category", category));
    }
}
