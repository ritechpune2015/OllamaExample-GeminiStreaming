using OllamaExample.Models;

namespace OllamaExample.Services.Conversation
{
    public interface IConversationService
    {
        Task<ChatResponse> SendMessageAsync(ChatRequest request);
        IAsyncEnumerable<string> StreamMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    }
}
