using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Cosmos;
using WildfireAgents.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WildfireAgents.Services
{
    public class CosmosDbService
    {
        private readonly Container _container;
        private readonly CosmosClient _cosmosClient;
        private readonly string databaseId = "WildfireDB";

        public CosmosDbService(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient; // Initialize _cosmosClient to resolve CS8618
            var database = _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).GetAwaiter().GetResult();
            _container = database.Database.GetContainer("defaultContainer"); // Provide a default container to resolve CS8618
        }

        public CosmosDbService(CosmosClient cosmosClient, string containerId)
        {
            _cosmosClient = cosmosClient; // Initialize _cosmosClient to resolve CS8618
            var database = _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).GetAwaiter().GetResult();
            var containerProperties = new ContainerProperties(containerId, "/regionId");
            _container = database.Database.CreateContainerIfNotExistsAsync(containerProperties).GetAwaiter().GetResult().Container;
        }

        public Container GetContainer(string containerId)
        {
            return _container.Database.GetContainer(containerId);
        }

        public async Task StoreSensorDataAsync(SensorData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        public async Task StoreRiskDataAsync(RiskData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        public async Task StoreSpreadData(SpreadData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        public async Task StoreImpactDataAsync(ImpactData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        public async Task StoreResponseDataAsync(ResponseData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        public async Task StoreSummaryDataAsync(SummaryData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        internal async Task StoreTranslationDataAsync(TranslationData data)
        {
            await _container.CreateItemAsync(data, new PartitionKey(data.RegionId));
        }

        public async Task<SensorData?> GetRecentSensorDataAsync(string regionId, Container container)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.RegionId = @regionId")
                .WithParameter("@regionId", regionId);

            var iterator = container.GetItemQueryIterator<SensorData>(query);
            var sensorData = (await iterator.ReadNextAsync()).FirstOrDefault();

            return sensorData;
        }
    }
}
