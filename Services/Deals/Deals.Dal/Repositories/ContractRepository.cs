using Dapper;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Dal.Repositories;

public sealed class ContractRepository(DbSession session) : BaseDapperRepository<Contract>(session), IContractRepository
{
    protected override string TableName => "dbo.Contracts";

    protected override string InsertSql => @"
INSERT INTO dbo.Contracts (Id, ApplicationId, FinalPrice, SignedAt, CreatedAt, CreatedBy)
VALUES (@Id, @ApplicationId, @FinalPrice, @SignedAt, SYSUTCDATETIME(), @CreatedBy)";

    protected override string UpdateSql => "UPDATE dbo.Contracts SET FinalPrice = @FinalPrice WHERE Id = @Id";

    public async Task<Contract?> GetByApplicationAsync(Guid applicationId)
    {
        var conn = await Session.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<Contract>(
            "SELECT * FROM dbo.Contracts WHERE ApplicationId = @applicationId",
            new { applicationId }, Session.Transaction);
    }
}
