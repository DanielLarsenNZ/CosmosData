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
    public class CosmosData<T> where T : ICosmosModel
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
            ItemResponse<T> response = await _container.CreateItemAsync(item);
            TrackEvent($"CosmosData/{TypeName}/Create", response);
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
            ItemResponse<T> response = await _container.DeleteItemAsync<T>(
                id,
                new PartitionKey(pk),
                new ItemRequestOptions
                {
                    IfMatchEtag = ifMatchETag,
                    EnableContentResponseOnWrite = true,
                });

            TrackEvent($"CosmosData/{TypeName}/Delete", response, id, pk, ifMatchETag);
        }

        /// <summary>
        /// Gets an item by Id/PK
        /// </summary>
        /// <param name="id">The item's Id</param>
        /// <param name="pk">The item's Partition Key</param>
        /// <returns>The item</returns>
        public async Task<T> Get(string id, string pk)
        {
            ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(pk));
            TrackEvent($"CosmosData/{TypeName}/Get", response);
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
            FeedResponse<T> items = await iterator.ReadNextAsync();
            TrackEvent($"CosmosData/{TypeName}/GetAll", items);
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
            TrackEvent($"CosmosData/{TypeName}/Replace", response);
            return response.Resource;
        }

        // Private helpers for tracking Cosmos DB metrics and events
        private void TrackEvent(string eventName, ItemResponse<T> response) =>
            TrackEvent(
                eventName, 
                response?.Resource?.Id, 
                response?.Resource?.PK, 
                response?.Resource?.ETag, 
                response.ActivityId, 
                response.RequestCharge, 
                response.Diagnostics.GetClientElapsedTime().TotalMilliseconds);

        private void TrackEvent(string eventName, FeedResponse<T> response) =>
            TrackEvent(
                eventName,
                null,
                null,
                null,
                response.ActivityId,
                response.RequestCharge,
                response.Diagnostics.GetClientElapsedTime().TotalMilliseconds);

        private void TrackEvent(string eventName, ItemResponse<T> response, string documentId, string documentPK, string documentETag = null) =>
            TrackEvent(
                eventName,
                documentId,
                documentPK,
                documentETag,
                response.ActivityId,
                response.RequestCharge,
                response.Diagnostics.GetClientElapsedTime().TotalMilliseconds);

        private void TrackEvent(string eventName, string documentId, string documentPK, string documentETag, string activityId, double requestCharge, double clientElapsedTimeMs) => 
            _telemetry.TrackEvent(
                eventName,
                properties: new Dictionary<string, string>
                    {
                        { "Cosmos_DocumentId", documentId },
                        { "Cosmos_DocumentPK", documentPK },
                        { "Cosmos_DocumentETag", documentETag },
                        { "Cosmos_ActivityId", activityId }
                    },
                metrics: new Dictionary<string, double>
                    {
                        { "Cosmos_RequestCharge", requestCharge },
                        { "Cosmos_ClientElapsedTime_TotalMilliseconds", clientElapsedTimeMs }
                    });
    }
}
