using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WildfireAgents.Models;
using WildfireAgents.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WildfireAgents.Functions;

public class FireRiskPredictorAgent
{
    private readonly ILogger<FireRiskPredictorAgent> _logger;
    private readonly OpenAIService _openAIService;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly ServiceBusDispatcher _serviceBusDispatcher;


    public FireRiskPredictorAgent(
        ILogger<FireRiskPredictorAgent> logger,
        OpenAIService openAIService,
        CosmosClient cosmosClient,
        ServiceBusDispatcher serviceBusDispatcher)
    {
        _logger = logger;
        _openAIService = openAIService;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "RiskData");
        _serviceBusDispatcher = serviceBusDispatcher;
    }

    [Function(nameof(FireRiskPredictorAgent))]
    public async Task Run(
        [ServiceBusTrigger("new-data", "fire-risk-predictor", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var sensorData = JsonSerializer.Deserialize<SensorData>(message.Body.ToString());

        if (sensorData != null)
        {
            _logger.LogInformation("Sensor Data: {data}", sensorData);

            var prompt = $"Predict wildfire risk score based on temperature: {sensorData.Temperature}, humidity: {sensorData.Humidity}, wind speed: {sensorData.WindSpeed}.";
            
            // var response =  await _openAIService.GetResponseAsync(prompt);

            // var riskScore = double.Parse(response);
            var riskScore = Random.Shared.NextDouble() * 100; // Simulating risk score for demonstration purposes

            var riskData = new RiskData 
            { 
                RegionId = sensorData.RegionId, 
                RiskScore = riskScore, 
                Timestamp = DateTime.UtcNow 
            };

            _logger.LogInformation("Risk Data: {data}", riskData);
            await _cosmosDbService.StoreRiskDataAsync(riskData);

            await _serviceBusDispatcher.DispatchAsync(riskData, "predicted-risk");

            _logger.LogInformation("Risk data stored and dispatched successfully.");

            await messageActions.CompleteMessageAsync(message);
        }
        else
        {
            _logger.LogWarning("Failed to deserialize sensor data from message.");

        }
    }
}