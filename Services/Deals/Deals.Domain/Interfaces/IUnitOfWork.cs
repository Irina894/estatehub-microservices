namespace Deals.Domain.Interfaces;

/// <summary>Одне з'єднання та одна транзакція, спільні для всіх репозиторіїв.</summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IApplicationRepository Applications { get; }
    IViewingRepository Viewings { get; }
    IContractRepository Contracts { get; }
    IRealtorRepository Realtors { get; }
    IApplicationRealtorRepository ApplicationRealtors { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
