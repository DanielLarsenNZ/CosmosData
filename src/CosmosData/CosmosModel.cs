using Newtonsoft.Json;

namespace CosmosData
{
    /// <summary>
    /// Model class that can be used as a base for any Cosmos DB Model class (DTO)
    /// </summary>
    public class CosmosModel : ICosmosModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("pk")]
        public string PK { get; set; }

        [JsonProperty("_etag")]
        public string ETag { get; set; }
    }
}
