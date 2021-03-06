using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CosmosDbIoTScenario.Common.Models;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

namespace Functions.StreamProcessing
{
    public static class Functions
    {
        [FunctionName("IoTHubTrigger")]
        public static async Task IoTHubTrigger([IoTHubTrigger("messages/events", Connection = "IoTHubConnection")] EventData[] vehicleEventData,
            [CosmosDB(
                databaseName: "ContosoAuto",
                collectionName: "telemetry",
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<VehicleEvent> vehicleTelemetryOut,
            ILogger log)
        {
            var exceptions = new List<Exception>();
            log.LogInformation($"IoT Hub trigger function processing {vehicleEventData.Length} events.");

            foreach (var eventData in vehicleEventData)
            {
                try
                {
                    var messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var vehicleEvent = JsonConvert.DeserializeObject<VehicleEvent>(messageBody);

                    // Update the partitionKey value.
                    // The partitionKey property represents a synthetic composite partition key for the
                    // Cosmos DB container, consisting of the VIN + current year/month. Using a composite
                    // key instead of simply the VIN provides us with the following benefits:
                    // (1) Distributing the write workload at any given point in time over a high cardinality
                    // of partition keys.
                    // (2) Ensuring efficient routing on queries on a given VIN - you can spread these across
                    // time, e.g. SELECT * FROM c WHERE c.partitionKey IN (�VIN123-2019-01�, �VIN123-2019-02�, �)
                    // (3) Scale beyond the 10GB quota for a single partition key value.
                    // TODO 7: Set the VehicleEvent partition key value as described in the comments above, set the TTL to 60 days, set the timestamp to now, then add it to the Cosmos DB output collection.
                    // Complete: vehicleEvent.partitionKey = ...; vehicleEvent.ttl = ...; vehicleEvent.timestamp = ...; await vehicleTelemetryOut ...;

                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
