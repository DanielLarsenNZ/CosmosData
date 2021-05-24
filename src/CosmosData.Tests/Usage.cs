using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosData.Tests
{
    [TestClass]
    public class Usage : CosmosDataTest
    {
        [TestMethod]
        public async Task Example()
        {
            using (var client = new CosmosClient(_config[CosmosDataConnectionStringKey]))
            {
                // new instance of Data helper
                var data = new CosmosData<Item>(_insights, client, databaseId: _config[CosmosDataDatabaseIdKey], containerId: _config[CosmosDataContainerIdKey]);

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

        [TestMethod]
        public async Task FilterExample()
        {
            using (var client = new CosmosClient(_config[CosmosDataConnectionStringKey]))
            {
                // new instance of Data helper
                var itemData = new ItemData(_config, _insights, client);

                // Create a red items
                await itemData.Create(new Item { Data = "Hello", Category = "Red" });
                await itemData.Create(new Item { Data = "Hello", Category = "Red" });

                // Create a blue items
                await itemData.Create(new Item { Data = "Hello", Category = "Blue" });
                await itemData.Create(new Item { Data = "Hello", Category = "Blue" });
                await itemData.Create(new Item { Data = "Hello", Category = "Blue" });

                // Get all red items
                var redItems = await itemData.GetFilteredByCategory(category: "Red");
                
                // Get all blue items
                var blueItems = await itemData.GetFilteredByCategory(category: "Blue");

                try
                {
                    // There should be 2
                    Assert.IsTrue(redItems.Count() == 2, "There should be 2 red items");

                    // There should be 3
                    Assert.IsTrue(blueItems.Count() == 3, "There should be 3 blue items");
                }
                finally
                {
                    // Delete red items
                    foreach (var item in redItems) await itemData.Delete(item.Id, item.PK, item.ETag);

                    // Delete red items
                    foreach (var item in blueItems) await itemData.Delete(item.Id, item.PK, item.ETag);
                }
            }
        }
    }

    public class Item : CosmosModel
    {
        public Item()
        {
            PK = Id = Guid.NewGuid().ToString("N");
        }

        public string Data { get; set; }

        public string Category { get; set; }
    }
}
