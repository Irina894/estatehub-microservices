using Deals.Dal.Repositories;
using Deals.Domain.Interfaces;

namespace Deals.Dal;

public sealed class UnitOfWork(string connectionString) : IUnitOfWork
{
    private readonly DbSession _session = new(connectionString);

    private IApplicationRepository? _applications;
    private IViewingRepository? _viewings;
    private IContractRepository? _contracts;
    private IRealtorRepository? _realtors;
    private IApplicationRealtorRepository? _applicationRealtors;

    // Applications - чистий ADO.NET; решта - Dapper (BaseDapperRepository<T>)
    public IApplicationRepository Applications => _applications ??= new ApplicationRepository(_session);
    public IViewingRepository Viewings => _viewings ??= new ViewingRepository(_session);
    public IContractRepository Contracts => _contracts ??= new ContractRepository(_session);
    public IRealtorRepository Realtors => _realtors ??= new RealtorRepository(_session);
    public IApplicationRealtorRepository ApplicationRealtors =>
        _applicationRealtors ??= new ApplicationRealtorRepository(_session);

    public Task BeginTransactionAsync() => _session.BeginAsync();
    public Task CommitAsync() => _session.CommitAsync();
    public Task RollbackAsync() => _session.RollbackAsync();

    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
