using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WildfireAgents.Models;
using WildfireAgents.Services;

namespace WildfireAgents.Functions;

public class FireSpreadSimulatorAgent
{
    private readonly ILogger<FireSpreadSimulatorAgent> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly ServiceBusDispatcher _servicebusDispatcher;

    public FireSpreadSimulatorAgent(
        ILogger<FireSpreadSimulatorAgent> logger,
        CosmosClient cosmosClient,
        ServiceBusDispatcher serviceBusDispatcher)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "SpreadData");
        _servicebusDispatcher = serviceBusDispatcher;
    }

    [Function(nameof(FireSpreadSimulatorAgent))]
    public async Task Run(
        [ServiceBusTrigger("predicted-risk", "fire-spread-simulator", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var riskData = JsonSerializer.Deserialize<RiskData>(message.Body.ToString());

        if (riskData != null)
        {
            _logger.LogInformation("Processing risk data for region: {regionId}, Risk Score: {riskScore}, Timestamp: {timestamp}",
                riskData.RegionId, riskData.RiskScore, riskData.Timestamp);

            var sensorData = await _cosmosDbService.GetRecentSensorDataAsync(riskData.RegionId, _cosmosDbService.GetContainer("SensorData"));

            // Simulate fire spread based on risk data and sensor data
            var spreadModel = new
            {
                Path = "POINT(34.0522 -118.2437),POINT(34.0523 -118.2440)" // Exemplo de caminho geoespacial
            };

            var spreadData = new SpreadData
            {
                RegionId = riskData.RegionId,
                SpreadPath = spreadModel.Path,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Storing spread data for region: {regionId}, Spread Path: {spreadPath}, Timestamp: {timestamp}",
                spreadData.RegionId, spreadData.SpreadPath, spreadData.Timestamp);

            await _cosmosDbService.StoreSpreadData(spreadData);

            await _servicebusDispatcher.DispatchAsync(spreadData, "fire-spread");

            // Complete the message
            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation("Fire spread data stored and dispatched successfully for region: {regionId}.", riskData.RegionId);
        }
        else
        {
            _logger.LogWarning("Failed to deserialize risk data from message.");
        }

    }
}