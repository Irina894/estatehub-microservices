using Deals.Domain.Interfaces;

namespace Deals.Bll.Services;

public static class UnitOfWorkExtensions
{
    /// <summary>Виконує роботу в одній транзакції: Commit при успіху, Rollback при будь-якому винятку.</summary>
    public static async Task<T> ExecuteInTransactionAsync<T>(this IUnitOfWork uow, Func<Task<T>> work)
    {
        await uow.BeginTransactionAsync();
        try
        {
            var result = await work();
            await uow.CommitAsync();
            return result;
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }
}
