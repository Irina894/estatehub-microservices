using Deals.Domain.Models;

namespace Deals.Domain.Interfaces;

public interface IApplicationRepository : IGenericRepository<Application>
{
    Task<PagedResult<Application>> GetPagedAsync(int page, int pageSize, string? status);
}

public interface IViewingRepository : IGenericRepository<Viewing>
{
    Task<IReadOnlyList<Viewing>> GetByApplicationAsync(Guid applicationId);
}

public interface IContractRepository : IGenericRepository<Contract>
{
    Task<Contract?> GetByApplicationAsync(Guid applicationId);
}

public interface IRealtorRepository : IGenericRepository<Realtor>
{
    Task<Realtor?> GetByEmailAsync(string email);
}

public interface IApplicationRealtorRepository
{
    Task AddAsync(ApplicationRealtor link);
    Task<bool> ExistsAsync(Guid applicationId, Guid realtorId);
    Task<IReadOnlyList<ApplicationRealtor>> GetByApplicationAsync(Guid applicationId);
}
