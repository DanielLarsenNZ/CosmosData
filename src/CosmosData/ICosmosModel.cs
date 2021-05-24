﻿using Newtonsoft.Json;

namespace CosmosData
{
    public interface ICosmosModel
    {
        [JsonProperty("id")]
        string Id { get; set; }

        [JsonProperty("pk")]
        string PK { get; set; }

        //string Type { get; set; }

        [JsonProperty("_etag")]
        string ETag { get; set; }
    }
}