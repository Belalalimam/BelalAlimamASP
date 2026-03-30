using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Repositories.IRepositories;
using System.Linq.Expressions;

namespace NakhlaBelal.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private ApplicationDbContext _context;// = new();
        private DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // CRUD

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet.AddAsync(entity, cancellationToken);
            return result.Entity;
        }
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default)
        {
            var entities = _dbSet.AsQueryable();

            if (expression is not null)
                entities = entities.Where(expression);

            if (includes is not null)
            {
                foreach (var item in includes)
                    entities = entities.Include(item);
            }

            if (!tracked)
                entities = entities.AsNoTracking();

            //entities = entities.Where(e => e.Status);

            return await entities.ToListAsync(cancellationToken);
        }

        public async Task<T?> GetOneAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default)
        {
            return (await GetAsync(expression, includes, tracked, cancellationToken)).FirstOrDefault();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        // Update existing entity - ADD THIS METHOD
        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await Task.CompletedTask; // Update doesn't need async but we keep the signature
        }

        // Count entities
        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync();
        }
        // Check if any entity matches condition
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.AnyAsync(filter);
        }

        // Find by primary key
        public async Task<T?> FindAsync(params object[] keyValues)
        {
            return await _dbSet.FindAsync(keyValues);
        }

    }
}
