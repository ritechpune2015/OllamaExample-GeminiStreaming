using Microsoft.EntityFrameworkCore;
using OllamaExample.Entities;

namespace OllamaExample.Services.Memory
{
    public class SqlMemoryStore : IMemoryStore
    {
        private readonly ApplicationDbContext _context;
        public SqlMemoryStore(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task AddAsync(MemoryItem memory)
        {
            await _context.MemoryItems
                .AddAsync(memory);
        }

        public async Task<List<MemoryItem>> GetAllAsync(string userId)
        {
            return await _context.MemoryItems
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Importance)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<MemoryItem?> GetByIdAsync(
            string userId,
            Guid memoryId)
        {
            return await _context.MemoryItems
                .FirstOrDefaultAsync(x =>
                    x.MemoryItemId == memoryId &&
                    x.UserId == userId);
        }

        public Task<MemoryItem?> FindExactAsync(
            string userId,
            string type,
            string content)
        {
            return _context.MemoryItems
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Type == type &&
                    x.Content == content);
        }

        public async Task DeleteAsync(
            string userId,
            Guid memoryId)
        {
            var memory =
                await GetByIdAsync(
                    userId,
                    memoryId);

            if (memory != null)
            {
                _context.MemoryItems.Remove(memory);
            }
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

    }
}