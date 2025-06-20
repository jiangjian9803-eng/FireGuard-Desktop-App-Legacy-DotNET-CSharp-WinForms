using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WildfireAgents.Models;
using WildfireAgents.Services;

namespace WildfireAgents.Functions;
public class DataCollectorAgent
{
    private readonly ILogger<DataCollectorAgent> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly ServiceBusDispatcher _serviceBusDispatcher;

    public DataCollectorAgent(
        ILogger<DataCollectorAgent> logger,
        CosmosClient cosmosClient,
        CosmosDbService cosmosDbService,
        ServiceBusDispatcher serviceBusDispatcher)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "SensorData");
        _serviceBusDispatcher = serviceBusDispatcher;
    }

    [Function(nameof(DataCollectorAgent))]
    public async Task Run([EventHubTrigger("%IOT_HUB_EVENTHUB_NAME%", Connection = "IOT_HUB_EVENTHUB_CONNECTIONSTRING")] EventData[] events)
    {
        foreach (EventData @event in events)
        {
            _logger.LogInformation("Event Body: {body}", @event.Body);
            _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);

            var sensorData = JsonSerializer.Deserialize<SensorData>(Encoding.UTF8.GetString(@event.Body.ToArray()));

            if (sensorData != null)
            {
                _logger.LogInformation("Sensor Data: {data}", sensorData);

                await _cosmosDbService.StoreSensorDataAsync(sensorData);

                await _serviceBusDispatcher.DispatchAsync(sensorData, "new-data");

                _logger.LogInformation("Sensor data stored and dispatched successfully.");
            }
            else
            {
                _logger.LogWarning("Failed to deserialize sensor data from event.");
            }
        }
    }
}