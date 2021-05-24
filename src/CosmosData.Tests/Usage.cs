using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CosmosData.Tests
{
    [TestClass]
    public class Usage
    {
        [TestMethod]
        public async Task Example()
        {
            // load configuration from appsettings.json file
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // initialise Application Insights telemetry client
            var insights = InsightsHelper.InitializeTelemetryClient(config["APPINSIGHTS_INSTRUMENTATIONKEY"]);

            using (var client = new CosmosClient(config["CosmosData_ConnectionString"]))
            {
                // new instance of Data helper
                ICosmosData<Item> data = new CosmosData<Item>(
                    config,
                    insights,
                    client);

                // Create
                var item = await data.Create(new Item { Data = "Hello" });

                // Get
                var getItem = await data.Get(item.Id, item.PK);

                // Get All 
                //TODO: Implement paging
                var getAllItems = await data.GetAll();

                // Replace / Update if-match
                var updateItem = await data.Replace(item, item.ETag);

                // Delete
                await data.Delete(updateItem.Id, updateItem.PK, updateItem.ETag);
            }
        }
    }

    public class Item : CosmosModel
    {
        public Item()
        {
            PK = Id = Guid.NewGuid().ToString("N");
            Type = nameof(Item);
        }

        public string Data { get; set; }
    }
}
