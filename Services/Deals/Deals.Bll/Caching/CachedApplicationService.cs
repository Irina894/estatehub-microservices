using Deals.Bll.DTOs;
using Deals.Bll.Services;

namespace Deals.Bll.Caching;

/// <summary>
/// Декоратор: кешується читання заявки за id (його часто викликає Aggregator через gRPC).
/// Заявка містить персональний контекст (ClientId) і змінюється при кожному переході статусу, тому TTL короткі,
/// а кожна операція запису синхронно видаляє ключ - інакше клієнт отримав би застарілий статус і RowVersion.
/// </summary>
public class CachedApplicationService(IApplicationService inner, ITwoLevelCache cache) : IApplicationService
{
    private static readonly CacheTtl ApplicationTtl = new(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60));

    // ---------- Читання ----------

    public async Task<ApplicationDto> GetAsync(Guid id) =>
        (await cache.GetOrCreateAsync<ApplicationDto>(
            DealsCacheKeys.Application(id), _ => inner.GetAsync(id), ApplicationTtl, CancellationToken.None))!;

    // Списки, договір, огляди й рієлтори: не кешуємо (змінюються частіше й залежать від багатьох параметрів)
    public Task<PagedResultDto<ApplicationDto>> GetPagedAsync(int page, int pageSize, string? status) =>
        inner.GetPagedAsync(page, pageSize, status);
    public Task<ContractDto> GetContractAsync(Guid id) => inner.GetContractAsync(id);
    public Task<IReadOnlyList<ApplicationRealtorDto>> GetRealtorsAsync(Guid id) => inner.GetRealtorsAsync(id);
    public Task<IReadOnlyList<ViewingDto>> GetViewingsAsync(Guid id) => inner.GetViewingsAsync(id);

    // ---------- Запис: операція → синхронна інвалідація ----------

    public Task<ApplicationDto> CreateAsync(CreateApplicationDto dto) => inner.CreateAsync(dto);   // нового ключа ще немає в кеші

    public async Task<ApplicationDto> ChangeStatusAsync(Guid id, ChangeApplicationStatusDto dto)
    {
        var result = await inner.ChangeStatusAsync(id, dto);
        await InvalidateAsync(id, "StatusChanged");
        return result;
    }

    public async Task<ViewingDto> ApproveAsync(Guid id, ApproveApplicationDto dto)
    {
        var result = await inner.ApproveAsync(id, dto);
        await InvalidateAsync(id, "ApplicationApproved");
        return result;
    }

    public async Task<ContractDto> SignContractAsync(Guid id, SignContractDto dto)
    {
        var result = await inner.SignContractAsync(id, dto);
        await InvalidateAsync(id, "ContractSigned");
        return result;
    }

    public async Task DeleteAsync(Guid id)
    {
        await inner.DeleteAsync(id);
        await InvalidateAsync(id, "ApplicationDeleted");
    }

    public Task<ApplicationRealtorDto> AssignRealtorAsync(Guid id, AssignRealtorDto dto) =>
        inner.AssignRealtorAsync(id, dto);   // ApplicationDto не змінюється

    private Task InvalidateAsync(Guid id, string reason) =>
        cache.RemoveAsync(DealsCacheKeys.Application(id), reason, CancellationToken.None);
}
