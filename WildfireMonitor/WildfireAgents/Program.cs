using Azure.AI.OpenAI;
using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WildfireAgents.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

//builder.Services
//    .AddApplicationInsightsTelemetryWorkerService()
//    .ConfigureFunctionsApplicationInsights();

var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultUrl");
if (string.IsNullOrEmpty(keyVaultUrl))
{
    throw new ArgumentNullException(nameof(keyVaultUrl), "KeyVaultUrl environment variable is not set.");
}

var keyVaultService = new KeyVaultService(keyVaultUrl);
builder.Services
    .AddSingleton(keyVaultService);

var cosmosDbConnectionString = await keyVaultService.GetSecretAsync("CosmosDbConnectionString");
var serviceBusConnectionString = await keyVaultService.GetSecretAsync("ServiceBusConnectionString");

var openAiServiceEndpoint = await keyVaultService.GetSecretAsync("OpenAIUrl");
var openAIServiceKey = await keyVaultService.GetSecretAsync("OpenAIKey");

builder.Services
    .AddSingleton(new CosmosClient(cosmosDbConnectionString))
    .AddSingleton<CosmosDbService>()
    .AddSingleton(new ServiceBusClient(serviceBusConnectionString))
    .AddSingleton<ServiceBusDispatcher>()
    .AddSingleton(new AzureOpenAIClient(
        new Uri(openAiServiceEndpoint),
        new AzureKeyCredential(openAIServiceKey)))
    .AddSingleton<OpenAIService>()
    .AddSingleton<AzureMapsService>();


builder.Build().Run();
