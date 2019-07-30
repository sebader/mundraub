using CsvHelper;
using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Spatial;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneTimeImporter
{
    class Program
    {
        private static HttpClient _httpClient = new HttpClient();
        static async Task Main(string[] args)
        {
            string cosmosdbUrl = args[0];
            string cosmosdbKey = args[1];
            string what3wordsKey = args[2];

            Console.WriteLine("ImportCsv function processed a request.");

            DocumentClient client = new DocumentClient(
                new Uri(cosmosdbUrl),
                cosmosdbKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp,
                    // Customize retry options for Throttled requests
                    RetryOptions = new RetryOptions()
                    {
                        MaxRetryAttemptsOnThrottledRequests = 10,
                        MaxRetryWaitTimeInSeconds = 30
                    }
                });

            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "MundraubDb" });
            var collection = await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("MundraubDb"), new DocumentCollection { Id = "PlantsCollection" });

            List<PlantImport> records;

            using (TextReader reader = new StreamReader(@"C:\temp\mundraub.csv"))

            using (var csv = new CsvReader(reader))
            {
                records = csv.GetRecords<PlantImport>().ToList();
            }

            Console.WriteLine($"Parsed {records.Count} records");

            var jsonPlants = JsonConvert.DeserializeObject<List<Plant>>(File.ReadAllText(@"C:\temp\plants.json"));

            int i = 0;

            var plants = new List<Plant>();
            //Parallel.ForEach(records, new ParallelOptions { MaxDegreeOfParallelism = 20 }, async (record) =>
            foreach (var record in records)
            {
                var plant = new Plant();
                plant.Description = record.Description;
                int count;
                if (int.TryParse(record.Count, out count))
                {
                    plant.Count = count;
                }
                DateTime date;
                if (DateTime.TryParseExact(record.CreatedAt, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    plant.CreatedAt = date;
                }
                plant.Title = record.Title;

                var r = new Regex(@"POINT \((?<lon>-?[0-9]{1,3}\.[0-9]{12}) (?<lat>-?[0-9]{1,3}\.[0-9]{12})\)");
                var m = r.Match(record.Coordinates);
                if (m.Success)
                {
                    plant.Lat = m.Groups["lat"].Value;
                    plant.Lon = m.Groups["lon"].Value;

                    var location = new Point(double.Parse(plant.Lon, CultureInfo.InvariantCulture), double.Parse(plant.Lat, CultureInfo.InvariantCulture));
                    plant.Location = location;

                    plant.Id = $"{plant.Title}_{plant.Lon},{plant.Lat}";
                    plants.Add(plant);

                    i++;
                    if (i % 100 == 0)
                    {
                        Console.WriteLine($"Added plant {i} to the list");
                        File.WriteAllText(@"C:\temp\plants_" + i +".json", JsonConvert.SerializeObject(plants));
                    }
                }
            }

            plants = jsonPlants.Where(r => string.IsNullOrEmpty(r.What3Words)).ToList();

            foreach(var plant in plants)
            {
                var url = $"https://api.what3words.com/v3/convert-to-3wa?key={what3wordsKey}&language=de&coordinates={plant.Lat},{plant.Lon}";
                try
                {
                    var response = await _httpClient.GetStringAsync(url);

                    var w3w = JsonConvert.DeserializeObject<What3WordsResponse>(response);

                    plant.What3Words = w3w.words;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception during w3w api call to?={url}");
                }
            }

            File.WriteAllText(@"C:\temp\plants.json", JsonConvert.SerializeObject(plants));

            Console.WriteLine($"Starting Cosmosdb import with {plants.Count}");

            IBulkExecutor bulkExecutor = new BulkExecutor(client, collection.Resource);
            await bulkExecutor.InitializeAsync();

            // Set retries to 0 to pass complete control to bulk executor.
            client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
            client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

            var result = await bulkExecutor.BulkImportAsync(plants, true);

            Console.WriteLine($"Finshed bulk import in {result.TotalTimeTaken}: {result.NumberOfDocumentsImported}, bad: {result.BadInputDocuments}. RUs: {result.TotalRequestUnitsConsumed}");
        }
    }
}
