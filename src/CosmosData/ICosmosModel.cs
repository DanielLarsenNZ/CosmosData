using Newtonsoft.Json;

namespace CosmosData
{
    /// <summary>
    /// Defines the minimum implementation for a Cosmos Model class (DTO). 
    /// </summary>
    public interface ICosmosModel
    {
        [JsonProperty("id")]
        string Id { get; set; }

        [JsonProperty("pk")]
        string PK { get; set; }

        [JsonProperty("_etag")]
        string ETag { get; set; }
    }
}