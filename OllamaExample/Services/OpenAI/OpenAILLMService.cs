using OllamaExample.Models;
using OllamaExample.Options;
using OpenAI.Chat;
using System.Runtime.CompilerServices;
using System.Text;

namespace OllamaExample.Services.OpenAI
{
    public class OpenAILLMService : ILLMService, IStreamingLLMService
    {
        private readonly ChatClient _chatClient;

        public OpenAILLMService(OpenAIOptions options)
        {
            _chatClient = new ChatClient(model: options.Model,apiKey: options.ApiKey);
        }

        public async Task<string> ChatAsync(List<LLMMessage> messages)
        {
            var chatMessages =
                new List<ChatMessage>();

           chatMessages= ConvertMessages(messages);
            

            ChatCompletion completion =await _chatClient.CompleteChatAsync(chatMessages);

            //return completion.Content[0].Text;
            return GetText(completion);
        }


        private static string GetText(ChatCompletion completion)
        {
            if (completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text))
            {
                throw new InvalidOperationException(
                    "OpenAI returned an empty response.");
            }

            return completion.Content[0].Text;
        }


        public async Task<string>   ChatStructuredAsync(List<LLMMessage> messages,string schemaName, string jsonSchema)
        {
            var chatMessages =ConvertMessages(messages);

            var options = new ChatCompletionOptions
                {
                    ResponseFormat =
                        ChatResponseFormat
                            .CreateJsonSchemaFormat(
                                jsonSchemaFormatName:
                                    schemaName,

                                jsonSchema:
                                    BinaryData.FromBytes(
                                        Encoding.UTF8
                                            .GetBytes(
                                                jsonSchema)),

                                jsonSchemaIsStrict:
                                    true)
                };

            ChatCompletion completion = await _chatClient
                    .CompleteChatAsync(
                        chatMessages,
                        options);

            return GetText(completion);
        }

        private static List<ChatMessage> ConvertMessages(
        List<LLMMessage> messages)
        {
            var result =
                new List<ChatMessage>();

            foreach (var message
                in messages)
            {
                switch (
                    message.Role
                        .ToLowerInvariant())
                {
                    case "system":

                        result.Add(   new SystemChatMessage(
                                message.Content));

                        break;

                    case "user":

                        result.Add(
                            new UserChatMessage(
                                message.Content));

                        break;

                    case "assistant":

                        result.Add(
                            new AssistantChatMessage(
                                message.Content));

                        break;
                }
            }

            return result;
        }



        public Task<string> GenerateAsync(string prompt)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<string> StreamChatAsync(
    List<LLMMessage> messages,
    [EnumeratorCancellation]
    CancellationToken cancellationToken = default)
        {
            var chatMessages = new List<ChatMessage>();

            foreach (var message in messages)
            {
                switch (message.Role.ToLowerInvariant())
                {
                    case "system":
                        chatMessages.Add(
                            new SystemChatMessage(message.Content));
                        break;

                    case "user":
                        chatMessages.Add(
                            new UserChatMessage(message.Content));
                        break;

                    case "assistant":
                        chatMessages.Add(
                            new AssistantChatMessage(message.Content));
                        break;

                    default:
                        throw new ArgumentException(
                            $"Unsupported message role: {message.Role}");
                }
            }

            var updates = _chatClient.CompleteChatStreamingAsync(
                chatMessages,
                cancellationToken: cancellationToken);

            await foreach (
                var update in updates.WithCancellation(cancellationToken))
            {
                foreach (var part in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                    {
                        yield return part.Text;
                    }
                }
            }
        }

    }
}
