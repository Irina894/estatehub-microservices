using Dapper;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Dal.Repositories;

public sealed class ViewingRepository(DbSession session) : BaseDapperRepository<Viewing>(session), IViewingRepository
{
    protected override string TableName => "dbo.Viewings";

    protected override string InsertSql => @"
INSERT INTO dbo.Viewings (Id, ApplicationId, RealtorId, [Date], Status, CreatedAt, CreatedBy)
VALUES (@Id, @ApplicationId, @RealtorId, @Date, @Status, SYSUTCDATETIME(), @CreatedBy)";

    protected override string UpdateSql => @"
UPDATE dbo.Viewings SET [Date] = @Date, Status = @Status WHERE Id = @Id";

    public async Task<IReadOnlyList<Viewing>> GetByApplicationAsync(Guid applicationId)
    {
        var conn = await Session.OpenAsync();
        var rows = await conn.QueryAsync<Viewing>(
            "SELECT * FROM dbo.Viewings WHERE ApplicationId = @applicationId ORDER BY [Date]",
            new { applicationId }, Session.Transaction);
        return rows.AsList();
    }
}
