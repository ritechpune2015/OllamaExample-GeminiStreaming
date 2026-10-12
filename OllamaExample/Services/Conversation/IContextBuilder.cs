using OllamaExample.Entities;
using OllamaExample.Models;

namespace OllamaExample.Services.Conversation
{
    public interface IContextBuilder
    {
        //List<OllamaMessage> BuildContext( Conversation conversation,
        //string currentUserMessage);

        Task<List<LLMMessage>>   BuildContextAsync(OllamaExample.Entities.Conversation conversation,
        string userId, string currentUserMessage);

    }
}
