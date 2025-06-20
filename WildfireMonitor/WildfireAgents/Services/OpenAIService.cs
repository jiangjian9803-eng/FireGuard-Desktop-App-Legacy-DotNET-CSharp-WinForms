using Azure.AI.OpenAI;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Identity.Client;
using OpenAI.Chat;
using WildfireAgents.Models;

namespace WildfireAgents.Services;

public class OpenAIService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deployment = "gpt-4o";

    public OpenAIService(AzureOpenAIClient client)
    {
        _client = client;
    }

    public async Task<string> GetResponseAsync(string prompt)
    {

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(prompt)
        };

        var response = await _client.GetChatClient(_deployment).CompleteChatAsync(messages, new ChatCompletionOptions
        {
            //Temperature = 0.4f,
            MaxOutputTokenCount = 500

        }).ConfigureAwait(false);

        return response.Value.Content[0].Text;

    }
}
