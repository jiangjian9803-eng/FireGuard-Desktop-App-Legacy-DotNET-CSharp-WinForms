using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI;
using WildfireAgents.Models;
using WildfireAgents.Services;
using Microsoft.Azure.Cosmos;

namespace WildfireAgents.Functions;

public class TranslatorAgent
{
    private readonly ILogger<TranslatorAgent> _logger;
    private readonly OpenAIService _openAIService;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;

    public TranslatorAgent(
        ILogger<TranslatorAgent> logger,
        OpenAIService openAIService,
        CosmosClient cosmosClient)
    {
        _logger = logger;
        _openAIService = openAIService;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "TranslationData");
    }

    [Function(nameof(TranslatorAgent))]
    public async Task Run(
        [ServiceBusTrigger("fire-summary", "fire-translator", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var summaryData = JsonSerializer.Deserialize<SummaryData>(message.Body.ToString());

        if (summaryData != null)
        {
            _logger.LogInformation("Deserialized SummaryData: {summaryData}", summaryData);

            var languages = new[] { "es", "fr", "pt" }; // Spanish, French, Portuguese
            var translations = new List<TranslationData>();

            foreach (var lang in languages)
            {
                var prompt = $"Translate the following summary into {lang}: {summaryData.Summary}";

                var translation = await _openAIService.GetResponseAsync(prompt);

                translations.Add(new TranslationData
                {
                    RegionId = summaryData.RegionId,
                    Language = lang,
                    TranslatedSummary = translation,
                    Timestamp = DateTime.UtcNow
                });
            }

            foreach (var translation in translations)
            {
                await _cosmosDbService.StoreTranslationDataAsync(translation);
            }

            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation("Translations stored successfully for region {regionId}.", summaryData.RegionId);
        }
        else
        {
            _logger.LogError("Failed to deserialize message body.");
            return;
        }


    }
}