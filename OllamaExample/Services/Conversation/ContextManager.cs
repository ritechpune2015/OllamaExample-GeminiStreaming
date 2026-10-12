using OllamaExample.Entities;
using OllamaExample.Models;
using OllamaExample.Services.Conversation;

namespace OllamaExample.Services.Context
{
    public class ContextManager : IContextManager
    {
        private const int RecentMessageCount = 10;

        private readonly IConversationSummaryService  _summaryService;

        public ContextManager(IConversationSummaryService summaryService)
        {
            _summaryService = summaryService;
        }

        public async Task<List<LLMMessage>>
            BuildContextAsync(
              OllamaExample.Entities.Conversation conversation,
                string currentUserMessage)
        {
            var messages =
                conversation.Messages
                    .OrderBy(x => x.CreatedAt)
                    .ToList();

            var recentMessages =
                messages
                    .TakeLast(RecentMessageCount)
                    .ToList();

            var result = new List<LLMMessage>();

            result.Add(new LLMMessage
            {
                Role = "system",
                Content =
                    """
                You are an expert C# and ASP.NET Core teacher.

                Explain concepts clearly.
                Use practical examples.
                Prefer C# examples.
                """
            });

            if (!string.IsNullOrWhiteSpace(
                conversation.Summary))
            {
                result.Add(new LLMMessage
                {
                    Role = "system",
                    Content =
                        $"Conversation summary:\n" +
                        conversation.Summary
                });
            }

            foreach (var message in recentMessages)
            {
                result.Add(new LLMMessage
                {
                    Role = message.Role,
                    Content = message.Content
                });
            }

            result.Add(new LLMMessage
            {
                Role = "user",
                Content = currentUserMessage
            });

            return result;
        }
    }

}
