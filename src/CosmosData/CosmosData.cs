using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosData
{
    /// <summary>
    /// A generic Cosmos DB Data access layer
    /// </summary>
    /// <typeparam name="T">A type of ICosmosModel</typeparam>
    public class CosmosData<T> : ICosmosData<T> where T : ICosmosModel
    {
        protected readonly Container _container;
        protected readonly string TypeName;
        protected readonly TelemetryClient _telemetry;

        public CosmosData(TelemetryClient telemetry, CosmosClient cosmos, string databaseId, string containerId)
        {
            _telemetry = telemetry;
            _container = cosmos.GetContainer(databaseId: databaseId, containerId: containerId);

            // Get type name. This is reflection on construction. CosmosData should be a singleton.
            var type = GetType();
            TypeName = type.GenericTypeArguments.Any() ? $"{nameof(CosmosData)}<{type.GenericTypeArguments[0].Name}>" : type.Name;
        }

        /// <summary>
        /// Create an item in the Cosmos DB container
        /// </summary>
        /// <param name="item">The item to create</param>
        /// <returns>The created resource</returns>
        public async Task<T> Create(T item)
        {
            var response = await _container.CreateItemAsync(item);
            TrackEvent($"CosmosData/{TypeName}/Create", response, item);
            return response.Resource;
        }

        /// <summary>
        /// Delete an item in the Cosmos DB container
        /// </summary>
        /// <param name="id">The item's Id</param>
        /// <param name="pk">The item's Partition Key</param>
        /// <param name="ifMatchETag">The ETag of the item to delete</param>
        public async Task Delete(string id, string pk, string ifMatchETag)
        {
            // Delete if-match eTag
            var response = await _container.DeleteItemAsync<T>(
                id,
                new PartitionKey(pk),
                new ItemRequestOptions
                {
                    IfMatchEtag = ifMatchETag
                });

            TrackEvent($"CosmosData/{TypeName}/Delete", response.RequestCharge);
        }

        /// <summary>
        /// Gets an item by Id/PK
        /// </summary>
        /// <param name="id">The item's Id</param>
        /// <param name="pk">The item's Partition Key</param>
        /// <returns>The item</returns>
        public async Task<T> Get(string id, string pk)
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(pk));
            TrackEvent($"CosmosData/{TypeName}/Get", response, response.Resource);
            return response.Resource;
        }

        /// <summary>
        /// Gets all items in the Cosmos DB container
        /// </summary>
        /// <returns>An IEnumerable of T</returns>
        public async Task<IEnumerable<T>> GetAll() => 
            await GetWithQuery(new QueryDefinition($"SELECT * FROM {_container.Id}"));

        /// <summary>
        /// Gets any items in the Cosmos DB container for the given Partition Key.
        /// </summary>
        /// <param name="pk">The Partition Key</param>
        /// <returns>An IEnumerable of T</returns>
        public async Task<IEnumerable<T>> GetFilteredByPartitionKey(string pk) => 
            await GetWithQuery(
                new QueryDefinition($"SELECT * FROM {_container.Id}"), 
                new QueryRequestOptions { PartitionKey = new PartitionKey(pk) });

        protected async Task<IEnumerable<T>> GetWithQuery(QueryDefinition query, QueryRequestOptions requestOptions = null)
        {
            var iterator = _container.GetItemQueryIterator<T>(query, requestOptions: requestOptions);
            var items = await iterator.ReadNextAsync();
            TrackEvent($"CosmosData/{TypeName}/GetAll", items.RequestCharge);
            return items.Resource;
        }

        /// <summary>
        /// Replaces an item if the ETag matches that provided.
        /// </summary>
        /// <param name="item">The new item</param>
        /// <param name="ifMatchEtag">The ETag of the item to be replaced.</param>
        /// <returns>The new item</returns>
        public async Task<T> Replace(T item, string ifMatchEtag)
        {
            // Replace if-match eTag
            var response = await _container.ReplaceItemAsync(
                item,
                item.Id,
                new PartitionKey(item.PK),
                new ItemRequestOptions
                {
                    IfMatchEtag = ifMatchEtag
                });
            TrackEvent($"CosmosData/{TypeName}/Replace", response, response.Resource);
            return response.Resource;
        }

        // Private helpers for tracking Cosmos DB metrics and events
        private void TrackEvent(string eventName, double requestCharge) => 
            _telemetry.TrackEvent(
                eventName,
                metrics: new Dictionary<string, double>
                {
                    { "Cosmos_RequestCharge", requestCharge }
                });

        private void TrackEvent(string eventName, ItemResponse<T> response, T item) => 
            _telemetry.TrackEvent(
                eventName,
                properties: new Dictionary<string, string>
                    {
                        { "Cosmos_DocumentId", item.Id },
                        { "Cosmos_DocumentPK", item.PK },
                        { "Cosmos_DocumentETag", response.ETag }
                    },
                metrics: new Dictionary<string, double>
                    {
                        { "Cosmos_RequestCharge", response.RequestCharge }
                    });
    }
}
