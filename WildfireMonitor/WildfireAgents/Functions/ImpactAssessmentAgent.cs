using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WildfireAgents.Models;
using WildfireAgents.Services;

namespace WildfireAgents.Functions;

public class ImpactAssessmentAgent
{
    private readonly ILogger<ImpactAssessmentAgent> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbService _cosmosDbService;
    private readonly ServiceBusDispatcher _serviceBusDispatcher;

    public ImpactAssessmentAgent(
        ILogger<ImpactAssessmentAgent> logger,
        CosmosClient cosmosClient,
        ServiceBusDispatcher serviceBusDispatcher
       )
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _cosmosDbService = new CosmosDbService(cosmosClient, "ImpactData");
        _serviceBusDispatcher = serviceBusDispatcher;
    }

    [Function(nameof(ImpactAssessmentAgent))]
    public async Task Run(
        [ServiceBusTrigger("fire-spread", "impact-assessed", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var spreadData = JsonSerializer.Deserialize<SpreadData>(message.Body.ToString());

        if (spreadData != null)
        {
            _logger.LogInformation("Processing spread data for region: {regionId}, Spread Path: {spreadPath}, Timestamp: {timestamp}",
                spreadData.RegionId, spreadData.SpreadPath, spreadData.Timestamp);

            var infrastructureData = GetInfrastructureForRegion(spreadData.RegionId);

            var impact = AssessImpact(spreadData, infrastructureData);

            var impactData = new ImpactData
            {
                RegionId = spreadData.RegionId,
                ImpactedAreas = impact.ImpactedAreas,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Assessing impact for region: {regionId}, Impacted Areas: {impactedAreas}, Timestamp: {timestamp}",
                impactData.RegionId, impactData.ImpactedAreas, impactData.Timestamp);

            await _cosmosDbService.StoreImpactDataAsync(impactData);

            await _serviceBusDispatcher.DispatchAsync(impactData, "impact-assessed");
            
            await messageActions.CompleteMessageAsync(message);
            _logger.LogInformation("Impact data stored and dispatched successfully for region: {regionId}", impactData.RegionId);
        }
        else
        {
            _logger.LogWarning("Failed to deserialize spread data from message.");

        }
    }


    private InfrastructureData GetInfrastructureForRegion(string regionId)
    {
        Dictionary<string, InfrastructureData> _data = new()
        {
            ["region-001"] = new InfrastructureData { RegionId = "region-001", InfrastructureDetails = "Hospital, Power Plant" },
            ["region-002"] = new InfrastructureData { RegionId = "region-002", InfrastructureDetails = "School, Highway" },
            ["region-003"] = new InfrastructureData { RegionId = "region-003", InfrastructureDetails = "Farm, Water Reservoir" },
        };

        {
            return _data.TryGetValue(regionId, out var infra)
                ? infra
                : new InfrastructureData { RegionId = regionId, InfrastructureDetails = "Unknown Infrastructure" };
        }

    }

    private ImpactData AssessImpact(SpreadData spread, InfrastructureData infra)
    {
        return new ImpactData
        {
            RegionId = spread.RegionId,
            ImpactedAreas = $"SpreadPath={spread.SpreadPath} | Affected={infra.InfrastructureDetails}",
            Timestamp = DateTime.UtcNow
        };
    }
}