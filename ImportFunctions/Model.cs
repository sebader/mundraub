using CsvHelper.Configuration.Attributes;
using Microsoft.Azure.Documents.Spatial;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MundraubFunctions
{
    public class Plant
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; } = null;

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
        public string Lat { get; set; }
        [JsonProperty("lon", NullValueHandling = NullValueHandling.Ignore)]
        public string Lon { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedAt { get; set; }
        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public int? Count { get; set; }
        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public Point Location { get; set; } = null;
        [JsonProperty("what3Words", NullValueHandling = NullValueHandling.Ignore)]
        public string What3Words { get; set; } = null;
    }


    public class PlantImport
    {

        [Name("title")]
        public string Title { get; set; }

        [Name("coordinates")]
        public string Coordinates { get; set; }

        [Name("description")]
        public string Description { get; set; }
        [Name("count")]
        public string Count { get; set; }

        [Name("created_at")]
        public string CreatedAt { get; set; }

    }



    public class What3WordsResponse
    {
        public string country { get; set; }
        public Square square { get; set; }
        public string nearestPlace { get; set; }
        public Coordinates coordinates { get; set; }
        public string words { get; set; }
        public string language { get; set; }
        public string map { get; set; }
    }

    public class Square
    {
        public Southwest southwest { get; set; }
        public Northeast northeast { get; set; }
    }

    public class Southwest
    {
        public float lng { get; set; }
        public float lat { get; set; }
    }

    public class Northeast
    {
        public float lng { get; set; }
        public float lat { get; set; }
    }

    public class Coordinates
    {
        public float lng { get; set; }
        public float lat { get; set; }
    }

}
