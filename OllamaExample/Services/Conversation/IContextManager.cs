using OllamaExample.Entities;
using OllamaExample.Models;

namespace OllamaExample.Services.Conversation
{
    public interface IContextManager
    {
        Task<List<LLMMessage>> BuildContextAsync(OllamaExample.Entities.Conversation conversation, string currentUserMessage);

    }
}
