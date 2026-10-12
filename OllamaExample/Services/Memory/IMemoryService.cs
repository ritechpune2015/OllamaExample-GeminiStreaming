using OllamaExample.Entities;

namespace OllamaExample.Services.Memory
{
    public interface IMemoryService
    {
        Task RememberAsync(string userId,string content, string type, int importance);

        Task<List<MemoryItem>> RecallAsync( string userId);

        Task<List<MemoryItem>> SearchAsync( string userId, string query);

        Task ForgetAsync(string userId,Guid memoryId);

    }
}
