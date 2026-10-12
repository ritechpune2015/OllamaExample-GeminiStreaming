using OllamaExample.Entities;
using OllamaExample.Models;

namespace OllamaExample.Services.Conversation
{
    public class ConversationSummaryService: IConversationSummaryService
    {
        private readonly ILLMService _llmService;

        public ConversationSummaryService(
            ILLMService llmService)
        {
            _llmService = llmService;
        }

        public async Task<string>
            GenerateSummaryAsync(
                string existingSummary,
                IReadOnlyCollection<ConversationMessage>
                    messagesToAdd)
        {
            if (messagesToAdd.Count == 0 &&
                string.IsNullOrWhiteSpace(
                    existingSummary))
            {
                return string.Empty;
            }

            var newConversationText =
                string.Join(
                    "\n\n",
                    messagesToAdd
                        .OrderBy(x => x.CreatedAt)
                        .Select(x =>
                            $"{x.Role}: {x.Content}"));

            var summaryPrompt =
                $"""
            Maintain a concise factual summary
            of this user's conversation.

            Existing summary:
            {(string.IsNullOrWhiteSpace(existingSummary)
                    ? "(none)"
                    : existingSummary)}

            New conversation messages:
            {newConversationText}

            Preserve:

            - User goals
            - Important technical decisions
            - Technologies being used
            - Important project information
            - Constraints
            - Relevant preferences
            - Unresolved questions

            Remove:

            - Greetings
            - Repetition
            - Unnecessary details

            Never invent information.

            Return only the updated summary.
            """;

            var messages =
                new List<LLMMessage>
                {
                new()
                {
                    Role = "system",

                    Content =
                        """
                        You maintain concise,
                        factual conversation summaries.

                        Never invent information.
                        """
                },

                new()
                {
                    Role = "user",

                    Content = summaryPrompt
                }
                };

            return await _llmService.ChatAsync(messages);
        }
    }
}