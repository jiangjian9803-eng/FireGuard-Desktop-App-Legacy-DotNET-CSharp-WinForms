using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace WildfireAgents.Services
{
    public class ServiceBusDispatcher
    {
        private readonly ServiceBusClient _client;

        public ServiceBusDispatcher(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task DispatchAsync<T>(T data, string topicName)
        {
            var sender = _client.CreateSender(topicName);
            var json = JsonSerializer.Serialize(data);
            var message = new ServiceBusMessage(json);
            await sender.SendMessageAsync(message);
        }
    }
}
