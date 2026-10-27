using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

/// <summary>Створює індекси при старті застосунку. CreateMany ідемпотентний (існуючі індекси не дублюються).</summary>
public sealed class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var keys = Builders<Review>.IndexKeys;
            var models = new[]
            {
                // 1. Складений: відгуки про об'єкт, найновіші першими
                new CreateIndexModel<Review>(
                    keys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt),
                    new CreateIndexOptions { Name = "ix_product_createdAt" }),

                // 2. Повнотекстовий пошук по тексту відгуку
                new CreateIndexModel<Review>(
                    keys.Text(r => r.Text),
                    new CreateIndexOptions { Name = "ix_text" }),

                // 3. Унікальний: один користувач = один відгук про об'єкт
                new CreateIndexModel<Review>(
                    keys.Ascending(r => r.ProductId).Ascending(r => r.Author.UserId),
                    new CreateIndexOptions { Name = "ux_product_author", Unique = true })
            };

            await context.Reviews.Indexes.CreateManyAsync(models, ct);
            logger.LogInformation("Індекси колекції reviews створено/перевірено ({Count}).", models.Length);
        }
        catch (Exception ex)
        {
            // додаток не падає, якщо MongoDB тимчасово недоступна
            logger.LogError(ex, "Не вдалося створити індекси (MongoDB недоступна?).");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
