
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogWebAPIApp.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        protected BlogContext _blogContext;

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

        public async Task<T?> Get(K key)
        {
            var item = await _blogContext.FindAsync<T>(key);
            return item != null ? item : null;
        }

        public async Task<IEnumerable<T>?> GetAll()
        {
            var items = await _blogContext.Set<T>().ToListAsync();
            if (items.Any())
                return items;
            return null;
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
    }
}
