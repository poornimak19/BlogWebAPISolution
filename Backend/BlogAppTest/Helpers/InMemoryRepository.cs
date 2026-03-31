using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace BlogAppTest.Helpers
{
    /// <summary>
    /// EF Core InMemory-backed IRepository implementation.
    /// GetQueryable() returns an EF-backed IQueryable so async LINQ extensions work.
    /// </summary>
    public class InMemoryRepository<K, T> : IRepository<K, T> where T : class
    {
        private readonly BlogContext _db;

        public InMemoryRepository(BlogContext db)
        {
            _db = db;
        }

        public async Task<T?> Add(T item)
        {
            _db.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task<T?> Delete(K key)
        {
            var item = await Get(key);
            if (item != null)
            {
                _db.Remove(item);
                await _db.SaveChangesAsync();
            }
            return item;
        }

        public async Task Delete(T item)
        {
            _db.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task<T?> Get(K key)
        {
            return await _db.FindAsync<T>(key);
        }

        public async Task<IEnumerable<T>?> GetAll()
        {
            var list = await _db.Set<T>().ToListAsync();
            return list.Any() ? list : null;
        }

        public async Task<T?> Update(K key, T item)
        {
            var existing = await Get(key);
            if (existing != null)
            {
                _db.Entry(existing).CurrentValues.SetValues(item);
                await _db.SaveChangesAsync();
                return existing;
            }
            return null;
        }

        public IQueryable<T> GetQueryable() => _db.Set<T>().AsQueryable();

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);

        // Convenience: seed data without triggering full Add flow
        public void Seed(IEnumerable<T> items)
        {
            _db.Set<T>().AddRange(items);
            _db.SaveChanges();
        }
    }

    /// <summary>
    /// Factory for creating isolated InMemory BlogContext instances per test.
    /// </summary>
    public static class TestDbContextFactory
    {
        public static BlogContext Create()
        {
            var opts = new DbContextOptionsBuilder<BlogContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new BlogContext(opts);
        }
    }
}
