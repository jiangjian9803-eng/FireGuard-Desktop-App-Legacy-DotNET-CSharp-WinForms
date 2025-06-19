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
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly TranslatorService _translatorService;

    public TranslatorAgent(
        ILogger<TranslatorAgent> logger,
        CosmosClient cosmosClient,
        TranslatorService translatorService)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "TranslationData");
        _translatorService = translatorService;
        _translatorService = translatorService;
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

            var languages = new[] { "es", "fr", "pt-br" }; // Spanish, French, Portuguese
            var translations = new List<TranslationData>();

            foreach (var lang in languages)
            {
                var translation = await _translatorService.TranslateAsync(summaryData.Summary, lang);

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