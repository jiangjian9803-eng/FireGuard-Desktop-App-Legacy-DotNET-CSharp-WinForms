using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WildfireAgents.Models;
using WildfireAgents.Services;

namespace WildfireAgents.Functions;

public class SummarizerAgent
{
    private readonly ILogger<SummarizerAgent> _logger;
    private readonly OpenAIService _openAIService;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly ServiceBusDispatcher _serviceBusDispatcher;


    public SummarizerAgent(
        ILogger<SummarizerAgent> logger,
        OpenAIService openAIService,
        CosmosClient cosmosClient,
        ServiceBusDispatcher serviceBusDispatcher)
    {
        _logger = logger;
        _openAIService = openAIService;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "SummaryData");
        _serviceBusDispatcher = serviceBusDispatcher;
    }

    [Function(nameof(SummarizerAgent))]
    public async Task Run(
        [ServiceBusTrigger("response-advised", "summary", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var responseData = JsonSerializer.Deserialize<ResponseData>(message.Body.ToString());

        if (responseData != null)
        {
            _logger.LogInformation("Processing response data for region: {regionId}, Recommendations: {recommendations}, Timestamp: {timestamp}",
                responseData.RegionId, responseData.Recommendations, responseData.Timestamp);

            var prompt = $"Summarize wildfire situation and recommendations for region {responseData.RegionId}: {responseData.Recommendations}.";

            var summaryResponse = await _openAIService.GetResponseAsync(prompt);

            var summaryData = new SummaryData { RegionId = responseData.RegionId, Summary = summaryResponse, Timestamp = DateTime.UtcNow };

            _logger.LogInformation("Summary for region: {regionId}, Summary: {summary}, Timestamp: {timestamp}",
                summaryData.RegionId, summaryData.Summary, summaryData.Timestamp);

            await _cosmosDbService.StoreSummaryDataAsync(summaryData);

            await _serviceBusDispatcher.DispatchAsync(summaryData, "fire-summary");

            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation("Summary data stored and dispatched successfully.");

        }
        else
        {
            _logger.LogWarning("Received null or invalid response data.");

        }
    }
}