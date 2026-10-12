using Google.GenAI;
using Google.GenAI.Types;
using OllamaExample.Models;
using OllamaExample.Options;
using System.Text.Json.Nodes;

namespace OllamaExample.Services.Gemini
{
    public class GeminiLLMService : ILLMService, IStreamingLLMService
    {
        private readonly Client _client;
        private readonly string _model;

        public GeminiLLMService(GeminiOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.Model))
            {
                throw new InvalidOperationException(
                    "Gemini model is not configured.");
            }

            _client = new Client(apiKey: options.ApiKey);
            _model = options.Model;
        }


        public async Task<string> ChatAsync(
        List<LLMMessage> messages)
        {
            var (systemInstruction, contents) =
                ConvertMessages(messages);

            var config = new GenerateContentConfig();

            if (!string.IsNullOrWhiteSpace(systemInstruction))
            {
                config.SystemInstruction = new Content
                {
                    Parts =
                    [
                     new Part
                    {
                        Text = systemInstruction
                    }
                    ]
                };
            }

            var response =
                await _client.Models.GenerateContentAsync(
                    model: _model,
                    contents: contents,
                    config: config);

            return ExtractText(response);
        }

        private static string ExtractText(GenerateContentResponse response)
        {
            //Candidates[0] - contains your required reponse 
                //Content
                 //Parts
                   //Text

            var text = response.Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .Where(part =>
                    !string.IsNullOrWhiteSpace(part.Text))
                .Select(part => part.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    "Gemini returned no text. " +
                    "The response may have been blocked or " +
                    "may not contain a text candidate.");
            }

            return text;
        }


        private static (string SystemInstruction,List<Content> Contents)
        ConvertMessages(List<LLMMessage> messages)
        {
            var systemMessages = new List<string>();
            var contents = new List<Content>();

            foreach (var message in messages)
            {
                switch (message.Role.ToLowerInvariant())
                {
                    case "system":
                        systemMessages.Add(message.Content);
                        break;

                    case "user":
                        contents.Add(new Content
                        {
                            Role = "user",
                            Parts =
                            [
                            new Part
                            {
                                Text = message.Content
                            }
                            ]
                        });
                        break;

                    case "assistant":
                        contents.Add(new Content
                        {
                            Role = "model",
                            Parts =
                            [
                                new Part
                            {
                                Text = message.Content
                            }
                            ]
                        });
                        break;

                    default:
                        throw new ArgumentException(
                            $"Unsupported LLM message role: {message.Role}");
                }
            }

            return (
                string.Join(
                    "\n\n",
                    systemMessages.Where(
                        x => !string.IsNullOrWhiteSpace(x))),
                contents);
        }

        public async Task<string> ChatStructuredAsync(List<LLMMessage> messages,string schemaName, string jsonSchema)
        {
            var (systemInstruction, contents) =
                ConvertMessages(messages);

            var config = new GenerateContentConfig
            {
                ResponseMimeType = "application/json",
                ResponseJsonSchema = JsonNode.Parse(jsonSchema)
            };

            if (!string.IsNullOrWhiteSpace(systemInstruction))
            {
                config.SystemInstruction = new Content
                {
                    Parts =
                    [
                        new Part
                    {
                        Text = systemInstruction
                    }
                    ]
                };
            }

            var response =
                await _client.Models.GenerateContentAsync(
                    model: _model,
                    contents: contents,
                    config: config);

            return ExtractText(response);
        }



        public Task<string> GenerateAsync(string prompt)
        {
            throw new NotImplementedException();
        }

    public async IAsyncEnumerable<string> StreamChatAsync(List<LLMMessage> messages,
       [System.Runtime.CompilerServices.EnumeratorCancellation]
 CancellationToken cancellationToken = default)
        {
            var (systemInstruction, contents) =
                ConvertMessages(messages);

            var config = new GenerateContentConfig();

            if (!string.IsNullOrWhiteSpace(systemInstruction))
            {
                config.SystemInstruction = new Content
                {
                    Parts =
                    [
                        new Part
                {
                    Text = systemInstruction
                }
                    ]
                };
            }

            await foreach (
                var chunk in _client.Models.GenerateContentStreamAsync(
                    model: _model,
                    contents: contents,
                    config: config,
                    cancellationToken: cancellationToken))
            {
                var text = chunk.Candidates?
                    .FirstOrDefault()?
                    .Content?
                    .Parts?
                    .Where(part =>
                        !string.IsNullOrEmpty(part.Text))
                    .Select(part => part.Text)
                    .ToList();

                if (text is null || text.Count == 0)
                {
                    continue;
                }

                foreach (var partText in text)
                {
                    if (!string.IsNullOrEmpty(partText))
                    {
                        yield return partText;
                    }
                }
            }
        }

    }
}
