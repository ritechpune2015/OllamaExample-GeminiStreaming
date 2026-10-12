using OllamaExample.Models;

namespace OllamaExample.Services
{
    public interface IStreamingLLMService
    {
        IAsyncEnumerable<string> StreamChatAsync(List<LLMMessage> messages,
    CancellationToken cancellationToken = default);

    }
}
