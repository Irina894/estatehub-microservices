using Deals.Domain.Models;

namespace Deals.Domain.Interfaces;

public interface IGenericRepository<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<Guid> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
}
