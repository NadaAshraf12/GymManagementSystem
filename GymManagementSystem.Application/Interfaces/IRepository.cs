using System.Linq.Expressions;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Query();
        Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<List<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);
        Task<TEntity?> FirstOrDefaultAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);
    }
}

