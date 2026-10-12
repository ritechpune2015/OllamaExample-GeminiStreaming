using OllamaExample.Entities;

namespace OllamaExample.Services.Memory
{
    public class MemoryService : IMemoryService
    {
        private const int MaxSearchResults = 5;

        private readonly IMemoryStore _memoryStore;

        public MemoryService(IMemoryStore memoryStore)
        {
            _memoryStore = memoryStore;
        }

        public async Task RememberAsync(string userId,string content,string type,int importance)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            importance = Math.Clamp(importance,1,5);

            type = string.IsNullOrWhiteSpace(type)
                ? "Fact"
                : type.Trim();

            content = content.Trim();

            // Prevent exact duplicates
            var existing =
                await _memoryStore.FindExactAsync(
                    userId,
                    type,
                    content);

            if (existing != null)
            {
                return;
            }

            var memory = new MemoryItem
            {
                MemoryItemId = Guid.NewGuid(),

                UserId = userId,

                Content = content,

                Type = type,

                Importance = importance,

                CreatedAt = DateTime.UtcNow
            };

            await _memoryStore.AddAsync(memory);

            await _memoryStore.SaveChangesAsync();
        }

        public Task<List<MemoryItem>> RecallAsync(string userId)
        {
            return _memoryStore.GetAllAsync(userId);
        }

        public async Task<List<MemoryItem>> SearchAsync( string userId,string query)
        {
            var memories = await _memoryStore
                    .GetAllAsync(userId);

            if (string.IsNullOrWhiteSpace(query))
            {
                return memories
                    .Take(MaxSearchResults)
                    .ToList();
            }

            var words = query
                .ToLowerInvariant()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            return memories
                .Select(memory => new
                {
                    Memory = memory,

                    Score = words.Count(word =>
                        memory.Content.Contains(
                            word,
                            StringComparison.OrdinalIgnoreCase))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(
                    x => x.Memory.Importance)
                .ThenByDescending(
                    x => x.Memory.CreatedAt)
                .Take(MaxSearchResults)
                .Select(x => x.Memory)
                .ToList();
        }

        public async Task ForgetAsync(
            string userId,
            Guid memoryId)
        {
            await _memoryStore.DeleteAsync(
                userId,
                memoryId);

            await _memoryStore.SaveChangesAsync();
        }
    }


}
