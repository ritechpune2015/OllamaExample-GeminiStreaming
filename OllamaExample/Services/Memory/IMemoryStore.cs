using OllamaExample.Entities;

namespace OllamaExample.Services.Memory
{
    public interface IMemoryStore
    {
        Task AddAsync(MemoryItem memory);

        Task<List<MemoryItem>> GetAllAsync(
            string userId);

        Task<MemoryItem?> GetByIdAsync(
            string userId,
            Guid memoryId);

        Task<MemoryItem?> FindExactAsync(
            string userId,
            string type,
            string content);

        Task DeleteAsync(
            string userId,
            Guid memoryId);

        Task SaveChangesAsync();
    }

}
