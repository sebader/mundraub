using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace MundraubFunctions
{
    public static class MundraubFunctions
    {
        [FunctionName("GetPlants")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")]
        DocumentClient client,
        ILogger log)
        {
            log.LogInformation("GetPlants function received a request.");

            string lat = req.Query["lat"];
            string lon = req.Query["lon"];
            string radius = req.Query["radius"];

            var queryString = "select r.title, r.description, r.lon, r.lat, r.what3Words, r.createdAt from plants r WHERE ST_DISTANCE(r.location, {'type': 'Point', 'coordinates':[" + lat + ", " + lon + "]}) < " + radius;

            IDocumentQuery<Plant> query = client.CreateDocumentQuery<Plant>(
                UriFactory.CreateDocumentCollectionUri("MundraubDb", "PlantsCollection"),
                queryString)
                .AsDocumentQuery();

            var results = new List<Plant>();
            while (query.HasMoreResults)
            {
                foreach (Plant result in await query.ExecuteNextAsync())
                {
                    log.LogInformation(result.Description);
                    results.Add(result);
                }
            }

            dynamic response = new { results = results.Count, plants = results };
            return new OkObjectResult(response);
        }
    }
}
