namespace OllamaExample.Services.Memory
{
    public interface IMemoryContextBuilder
    {
        Task<string> BuildMemoryContextAsync(string userId,string userMessage);
    }
}
