using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WildfireAgents.Models;
using WildfireAgents.Services;

namespace WildfireAgents.Functions;

public class ResponseAdvisorAgent
{
    private readonly ILogger<ResponseAdvisorAgent> _logger;
    private readonly OpenAIService _openAIService;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly ServiceBusDispatcher _serviceBusDispatcher;

    public ResponseAdvisorAgent(
        ILogger<ResponseAdvisorAgent> logger,
        OpenAIService openAIService,
        CosmosClient cosmosClient,
        ServiceBusDispatcher serviceBusDispatcher
        )
    {
        _logger = logger;
        _openAIService = openAIService;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "ResponseData");
        _serviceBusDispatcher = serviceBusDispatcher;
    }

    [Function(nameof(ResponseAdvisorAgent))]
    public async Task Run(
        [ServiceBusTrigger("impact-assessed", "fire-response", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var impactData = JsonSerializer.Deserialize<ImpactData>(message.Body.ToString());

        if (impactData != null)
        {
            _logger.LogInformation("Processing impact data for region: {regionId}, Impacted Areas: {impactedAreas}, Timestamp: {timestamp}",
                impactData.RegionId, impactData.ImpactedAreas, impactData.Timestamp);

            var prompt = $"Recommend resource allocation and evacuation for impacted areas: {impactData.ImpactedAreas}.";
            var response = await _openAIService.GetResponseAsync(prompt);

            var responseData = new ResponseData
            {
                RegionId = impactData.RegionId,
                Recommendations = response,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Response recommendations for region: {regionId}, Recommendations: {recommendations}, Timestamp: {timestamp}",
                responseData.RegionId, responseData.Recommendations, responseData.Timestamp);

            await _cosmosDbService.StoreResponseDataAsync(responseData);

            await _serviceBusDispatcher.DispatchAsync(responseData, "response-advised");

            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation("Response data stored and dispatched successfully.");
        }
        else
        {
            _logger.LogWarning("Failed to deserialize impact data from message.");
        }
    }
}