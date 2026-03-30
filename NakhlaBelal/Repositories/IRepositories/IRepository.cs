using System.Linq.Expressions;

namespace NakhlaBelal.Repositories.IRepositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);

        Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default);

        Task<T?> GetOneAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default);
        // Add this UpdateAsync method
        Task UpdateAsync(T entity);

        Task CommitAsync(CancellationToken cancellationToken = default);

        // Utility Methods
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
        Task<T?> FindAsync(params object[] keyValues);
    }
}
