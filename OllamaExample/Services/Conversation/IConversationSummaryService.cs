using OllamaExample.Entities;

namespace OllamaExample.Services.Conversation
{
    public interface IConversationSummaryService
    {
        Task<string> GenerateSummaryAsync(string existingSummary,
        IReadOnlyCollection<ConversationMessage>
            messagesToAdd);
    }
}
