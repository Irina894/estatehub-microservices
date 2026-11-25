using Dapper;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Dal.Repositories;

public sealed class ApplicationRealtorRepository(DbSession session) : IApplicationRealtorRepository
{
    public async Task AddAsync(ApplicationRealtor link)
    {
        var conn = await session.OpenAsync();
        await conn.ExecuteAsync(@"
INSERT INTO dbo.ApplicationRealtors (ApplicationId, RealtorId, Role, AssignedAt)
VALUES (@ApplicationId, @RealtorId, @Role, SYSUTCDATETIME())", link, session.Transaction);
    }

    public async Task<bool> ExistsAsync(Guid applicationId, Guid realtorId)
    {
        var conn = await session.OpenAsync();
        return await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM dbo.ApplicationRealtors
WHERE ApplicationId = @applicationId AND RealtorId = @realtorId",
            new { applicationId, realtorId }, session.Transaction) > 0;
    }

    public async Task<IReadOnlyList<ApplicationRealtor>> GetByApplicationAsync(Guid applicationId)
    {
        var conn = await session.OpenAsync();
        var rows = await conn.QueryAsync<ApplicationRealtor>(
            "SELECT * FROM dbo.ApplicationRealtors WHERE ApplicationId = @applicationId",
            new { applicationId }, session.Transaction);
        return rows.AsList();
    }
}
