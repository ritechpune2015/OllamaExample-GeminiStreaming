using OllamaExample.Models;

namespace OllamaExample.Services
{
    public interface ILLMService
    {
        Task<string> GenerateAsync(string prompt);
        Task<string> ChatAsync(List<LLMMessage> messages);
        Task<string> ChatStructuredAsync(List<LLMMessage> messages,string schemaName,string jsonSchema);
    }
}
