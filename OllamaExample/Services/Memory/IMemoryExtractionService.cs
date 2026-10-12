namespace OllamaExample.Services.Memory
{
    public interface IMemoryExtractionService
    {
        Task ExtractAndStoreAsync(string userId, string userMessage,string assistantResponse);
    }
}
