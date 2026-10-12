using Microsoft.EntityFrameworkCore;
using OllamaExample.Entities;

namespace OllamaExample.Repositories
{
    public class ConversationRepository: IConversationRepository
    {
        private readonly ApplicationDbContext _context;

        public ConversationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Conversation?>  GetConversationAsync(Guid conversationId,string userId)
        {
            return _context.Conversations
                .Include(x => x.Messages)
                .FirstOrDefaultAsync(
                    x =>
                        x.ConversationId ==
                            conversationId
                        &&
                        x.UserId ==
                            userId);
        }

        public async Task<Conversation>
            CreateConversationAsync(
                string userId,
                string title)
        {
            var conversation =
                new Conversation
                {
                    ConversationId =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    Title =
                        title,

                    CreatedAt =
                        DateTime.UtcNow,

                    UpdatedAt =
                        DateTime.UtcNow
                };

            _context.Conversations
                .Add(conversation);

            await _context
                .SaveChangesAsync();

            return conversation;
        }

        public async Task AddMessageAsync(
            Guid conversationId,
            string role,
            string content)
        {
            var message =
                new ConversationMessage
                {
                    ConversationId =
                        conversationId,

                    Role =
                        role,

                    Content =
                        content,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.ConversationMessages
                .Add(message);

            var conversation =
                await _context.Conversations.FirstOrDefaultAsync(x =>
                            x.ConversationId ==
                            conversationId);

            if (conversation != null)
            {
                conversation.UpdatedAt =
                    DateTime.UtcNow;
            }
        }

        public async Task UpdateSummaryAsync(
            Guid conversationId,
            string summary,
            int summaryMessageCount)
        {
            var conversation =
                await _context.Conversations
                    .FirstOrDefaultAsync(
                        x =>
                            x.ConversationId ==
                            conversationId);

            if (conversation == null)
            {
                throw new InvalidOperationException(
                    "Conversation not found.");
            }

            conversation.Summary =
                summary;

            conversation.SummaryMessageCount =
                summaryMessageCount;

            conversation.SummaryUpdatedAt =
                DateTime.UtcNow;

            conversation.UpdatedAt =
                DateTime.UtcNow;
        }

        public Task SaveChangesAsync()
        {
            return _context
                .SaveChangesAsync();
        }
    }
}