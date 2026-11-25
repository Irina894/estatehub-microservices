using Deals.Api.IntegrationEvents;
using Microsoft.AspNetCore.Mvc;

namespace Deals.Api.Controllers;

/// <summary>
/// Лаб. 8: лише читання локальної копії довідника Properties, яку Deals підтримує через події Каталогу.
/// Потрібен, щоб побачити (і показати викладачу), як за кілька секунд після зміни ціни/назви в Каталозі
/// змінюється копія, тоді як ціни у вже поданих заявках лишаються незмінними.
/// </summary>
[ApiController]
[Route("api/property-snapshots")]
public class PropertySnapshotsController(PropertySnapshotStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertySnapshotRow>>> GetAll(CancellationToken ct) =>
        Ok(await store.ListAsync(ct));
}
