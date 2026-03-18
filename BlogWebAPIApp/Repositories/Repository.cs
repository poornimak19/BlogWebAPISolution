using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogWebAPIApp.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        protected readonly BlogContext _blogContext;

        public Repository(BlogContext blogContext)
        {
            _blogContext = blogContext;
        }

        public async Task<T?> Add(T item)
        {
            _blogContext.Add(item);
            await _blogContext.SaveChangesAsync();
            return item;
        }

        public async Task<T?> Delete(K key)
        {
            var item = await Get(key);
            if (item != null)
            {
                _blogContext.Remove(item);
                await _blogContext.SaveChangesAsync();
                return item;
            }
            return null;
        }

        // For composite keys or when you already have the entity instance
        public async Task Delete(T item)
        {
            _blogContext.Remove(item);
            await _blogContext.SaveChangesAsync();
        }

        public async Task<T?> Get(K key)
        {
            var item = await _blogContext.FindAsync<T>(key);
            return item;
        }

        public async Task<IEnumerable<T>?> GetAll()
        {
            var items = await _blogContext.Set<T>().ToListAsync();
            return items.Any() ? items : null;
        }

        public async Task<T?> Update(K key, T item)
        {
            var existingItem = await Get(key);
            if (existingItem != null)
            {
                _blogContext.Entry(existingItem).CurrentValues.SetValues(item);
                await _blogContext.SaveChangesAsync();
                return existingItem;
            }
            return null;
        }

        public IQueryable<T> GetQueryable()
        {
            return _blogContext.Set<T>().AsQueryable();
        }

        // 🔧 Missing implementation added
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return _blogContext.SaveChangesAsync(ct);
        }
    }
}