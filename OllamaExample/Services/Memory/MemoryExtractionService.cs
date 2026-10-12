using OllamaExample.Models;
using System.Text.Json;

namespace OllamaExample.Services.Memory
{
    public class MemoryExtractionService : IMemoryExtractionService
    {
        private readonly ILLMService _llmService;

        private readonly IMemoryService  _memoryService;

        private const string JsonSchema  = 
        """
        {
          "type": "object",

          "properties": {

            "shouldRemember": {
              "type": "boolean"
            },

            "memories": {

              "type": "array",

              "items": {

                "type": "object",

                "properties": {

                  "content": {
                    "type": "string"
                  },

                  "type": {
                    "type": "string"
                  },

                  "importance": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 5
                  }

                },

                "required": [
                  "content",
                  "type",
                  "importance"
                ],

                "additionalProperties": false
              }
            }
          },

          "required": [
            "shouldRemember",
            "memories"
          ],

          "additionalProperties": false
        }
        """;

        public MemoryExtractionService(ILLMService llmService,IMemoryService memoryService)
        {
            _llmService = llmService;

            _memoryService =  memoryService;
        }

        public async Task    ExtractAndStoreAsync( string userId,
                string userMessage,
                string assistantResponse)
        {
            var messages = new List<LLMMessage>
                {
                new()
                {
                    Role = "system",

                    Content =
                        """
                        You are a memory extraction
                        component for an AI assistant.

                        Identify only durable information
                        that would be useful in future
                        conversations with this user.

                        Good memories include:

                        - User preferences
                        - Long-running projects
                        - Important technical choices
                        - Stable goals
                        - Important constraints

                        Do NOT remember:

                        - Greetings
                        - Temporary questions
                        - Assistant explanations
                        - Generic technical facts
                        - Passwords
                        - API keys
                        - Tokens
                        - Credentials
                        - One-time details with no future value

                        Keep each memory short and factual.
                        """
                },

                new()
                {
                    Role = "user",

                    Content =
                        $"""
                        User message:

                        {userMessage}

                        Assistant response:

                        {assistantResponse}

                        Extract durable user-specific
                        memories from the USER message.

                        The assistant response is provided
                        only for context.
                        """
                }
                };

            var json = await _llmService.ChatStructuredAsync(messages,"memory_extraction",JsonSchema);

            var result =
                JsonSerializer.Deserialize
                    <MemoryExtractionResult>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive =
                                true
                        });

            if (result == null ||
                !result.ShouldRemember)
            {
                return;
            }

            foreach (
                var memory
                in result.Memories)
            {
                if (string.IsNullOrWhiteSpace(
                    memory.Content))
                {
                    continue;
                }

                await _memoryService
                    .RememberAsync(
                        userId,
                        memory.Content,
                        memory.Type,
                        memory.Importance);
            }
        }
    }

}
