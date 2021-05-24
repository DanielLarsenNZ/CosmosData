using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosData
{
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

        public async Task<T> Create(T item)
        {
            var response = await _container.CreateItemAsync(item);
            TrackEvent($"CosmosData/{TypeName}/Create", response, item);
            return response.Resource;
        }

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

        public async Task<T> Get(string id, string pk)
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(pk));
            TrackEvent($"CosmosData/{TypeName}/Get", response, response.Resource);
            return response.Resource;
        }

        public async Task<IEnumerable<T>> GetAll() => 
            await GetWithQuery(new QueryDefinition($"SELECT * FROM {_container.Id}"));

        protected async Task<IEnumerable<T>> GetWithQuery(QueryDefinition query, QueryRequestOptions requestOptions = null)
        {
            var iterator = _container.GetItemQueryIterator<T>(query, requestOptions: requestOptions);
            var items = await iterator.ReadNextAsync();
            TrackEvent($"CosmosData/{TypeName}/GetAll", items.RequestCharge);
            return items.Resource;
        }

        public async Task<IEnumerable<T>> GetFilteredByPartitionKey(string pk) => 
            await GetWithQuery(
                new QueryDefinition($"SELECT * FROM {_container.Id}"), 
                new QueryRequestOptions { PartitionKey = new PartitionKey(pk) });

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
