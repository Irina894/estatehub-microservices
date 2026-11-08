using Dapper;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Dal.Repositories;

public sealed class RealtorRepository(DbSession session) : BaseDapperRepository<Realtor>(session), IRealtorRepository
{
    protected override string TableName => "dbo.Realtors";

    protected override string InsertSql => @"
INSERT INTO dbo.Realtors (Id, FullName, Email, Phone, IsActive, CreatedAt, CreatedBy)
VALUES (@Id, @FullName, @Email, @Phone, @IsActive, SYSUTCDATETIME(), @CreatedBy)";

    protected override string UpdateSql => @"
UPDATE dbo.Realtors SET FullName = @FullName, Email = @Email, Phone = @Phone, IsActive = @IsActive
WHERE Id = @Id";

    // Рієлтор пов'язаний з історією заявок, тому фізично не видаляється - лише деактивується.
    public override async Task<bool> DeleteAsync(Guid id)
    {
        var conn = await Session.OpenAsync();
        return await conn.ExecuteAsync(
            "UPDATE dbo.Realtors SET IsActive = 0 WHERE Id = @id", new { id }, Session.Transaction) > 0;
    }

    public async Task<Realtor?> GetByEmailAsync(string email)
    {
        var conn = await Session.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<Realtor>(
            "SELECT * FROM dbo.Realtors WHERE Email = @email", new { email }, Session.Transaction);
    }
}
