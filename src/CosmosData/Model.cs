using Newtonsoft.Json;

namespace CosmosData
{
    public class Model : IModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("pk")]
        public string PK { get; set; }

        [JsonProperty("_etag")]
        public string ETag { get; set; }

        public string Type { get; set; }
    }
}
