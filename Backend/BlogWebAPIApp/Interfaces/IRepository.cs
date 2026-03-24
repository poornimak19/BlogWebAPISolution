using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlogWebAPIApp.Interfaces
{
    public interface IRepository<K, T> where T : class
    {
        Task<T?> Get(K key);
        Task<IEnumerable<T>?> GetAll();
        Task<T?> Add(T item);
        Task<T?> Update(K key, T item);
        Task<T?> Delete(K key);

        // Needed for composite keys and tracked entity deletions
        Task Delete(T item);

        IQueryable<T> GetQueryable();

        // Allow services to commit tracked changes when needed
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}