using OllamaExample.Entities;

namespace OllamaExample.Repositories
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetConversationAsync(Guid conversationId,string userId);

        Task<Conversation> CreateConversationAsync(string userId, string title);

        Task AddMessageAsync(
            Guid conversationId,
            string role,
            string content);

        Task UpdateSummaryAsync(
            Guid conversationId,
            string summary,
            int summaryMessageCount);

        Task SaveChangesAsync();


    }
}
