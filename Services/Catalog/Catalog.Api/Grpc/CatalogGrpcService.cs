using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using Grpc.Core;

namespace Catalog.Api.Grpc;

/// <summary>
/// gRPC-адаптер над BLL (як контролер): жодної бізнес-логіки, лише валідація входу, виклик сервісу й перетворення
/// моделей у повідомлення. Сервіси - це ті самі (кешовані) <see cref="IPropertyService"/> / <see cref="ICategoryService"/>,
/// що й у REST-контролерах, тому кешування працює для обох протоколів без дублювання коду.
/// </summary>
public class CatalogGrpcService(IPropertyService properties, ICategoryService categories)
    : CatalogService.CatalogServiceBase
{
    private const int MaxIdsPerCall = 100;

    public override async Task<PropertyReply> GetProperty(GetPropertyRequest request, ServerCallContext context)
    {
        var id = ParseId(request.Id);
        try
        {
            return ToReply(await properties.GetByIdAsync(id, context.CancellationToken));
        }
        catch (EntityNotFoundException ex)
        {
            // доменний виняток -> правильний статус, а не Unknown/Internal
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<PropertiesReply> GetPropertiesByIds(GetPropertiesByIdsRequest request, ServerCallContext context)
    {
        if (request.Ids.Count == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one id is required"));
        if (request.Ids.Count > MaxIdsPerCall)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"At most {MaxIdsPerCall} ids per call"));

        var reply = new PropertiesReply();
        // послідовно: DbContext не потокобезпечний, а попадання в кеш і так швидкі
        foreach (var id in request.Ids.Select(ParseId).Distinct())
        {
            try
            {
                reply.Properties.Add(ToReply(await properties.GetByIdAsync(id, context.CancellationToken)));
            }
            catch (EntityNotFoundException)
            {
                // відсутній об'єкт пропускаємо: пакетний запит не падає через один id
            }
        }
        return reply;
    }

    public override async Task<CategoriesReply> ListCategories(ListCategoriesRequest request, ServerCallContext context)
    {
        var reply = new CategoriesReply();
        foreach (var c in await categories.GetAllAsync(context.CancellationToken))
            reply.Categories.Add(new CategoryReply { Id = c.Id.ToString(), Name = c.Name });
        return reply;
    }

    private static Guid ParseId(string value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{value}' is not a valid GUID"));

    private static PropertyReply ToReply(PropertyDetailsDto p)
    {
        var reply = new PropertyReply
        {
            Id = p.Id.ToString(),
            Title = p.Title,
            Description = p.Description,
            Price = (double)p.Price,
            City = p.City,
            Status = p.Status,
            CategoryName = p.CategoryName
        };

        if (p.Location is { } l)
        {
            var location = new PropertyLocation { FullAddress = l.FullAddress };
            if (l.Latitude is { } lat) location.Latitude = lat;
            if (l.Longitude is { } lon) location.Longitude = lon;
            reply.Location = location;
        }
        return reply;
    }
}
