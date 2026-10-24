using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Specifications;
using Catalog.Dal.Repositories;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

// Сервіс працює ТІЛЬКИ через IUnitOfWork (DbContext напряму не використовується)
public class PropertyService(IUnitOfWork uow, IMapper mapper) : IPropertyService
{
    public async Task<PagedResult<PropertyDto>> GetPagedAsync(PropertyQuery query, CancellationToken ct = default)
    {
        // COUNT(*) із тими самими фільтрами + сторінка (Skip/Take) — обидва виконуються в SQL
        var total = await uow.Properties.CountAsync(new PropertiesFilterSpec(query), ct);
        var items = await uow.Properties.ListAsync(new PropertiesPageSpec(query), ct);

        return new PagedResult<PropertyDto>(
            mapper.Map<List<PropertyDto>>(items),
            total,
            query.NormalizedPage,
            query.NormalizedPageSize);
    }

    public async Task<PropertyDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var property = await uow.Properties.GetWithDetailsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Property), id);
        return mapper.Map<PropertyDetailsDto>(property);
    }

    public async Task<PropertyDetailsDto> CreateAsync(CreatePropertyDto dto, CancellationToken ct = default)
    {
        if (await uow.Categories.GetByIdAsync(dto.CategoryId, ct) is null)
            throw new EntityNotFoundException(nameof(Category), dto.CategoryId);

        if (await uow.Properties.ExistsByOwnerAndTitleAsync(dto.OwnerId, dto.Title.Trim(), null, ct))
            throw new ConflictException($"Власник уже має об'єкт з назвою '{dto.Title}'.");

        // Бізнес-правила перевіряє сама сутність (кине DomainException)
        var property = new Property(dto.CategoryId, dto.OwnerId, dto.Title, dto.Description, dto.Price, dto.City);

        if (dto.Location is not null)
            property.SetLocation(dto.Location.FullAddress, dto.Location.Latitude, dto.Location.Longitude);

        foreach (var url in dto.ImageUrls)
            property.AddImage(url);

        await uow.Properties.AddAsync(property, ct);
        await uow.SaveChangesAsync(ct);

        return await GetByIdAsync(property.Id, ct);
    }

    public async Task UpdateAsync(Guid id, UpdatePropertyDto dto, CancellationToken ct = default)
    {
        var property = await GetTrackedAsync(id, ct);

        if (await uow.Categories.GetByIdAsync(dto.CategoryId, ct) is null)
            throw new EntityNotFoundException(nameof(Category), dto.CategoryId);

        if (await uow.Properties.ExistsByOwnerAndTitleAsync(property.OwnerId, dto.Title.Trim(), id, ct))
            throw new ConflictException($"Власник уже має інший об'єкт з назвою '{dto.Title}'.");

        property.UpdateDetails(dto.CategoryId, dto.Title, dto.Description, dto.City);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ChangePriceAsync(Guid id, ChangePriceDto dto, CancellationToken ct = default)
    {
        var property = await GetTrackedAsync(id, ct);
        property.ChangePrice(dto.Price);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken ct = default)
    {
        var property = await GetTrackedAsync(id, ct);
        var status = Enum.Parse<PropertyStatus>(dto.Status, ignoreCase: true);   // формат уже перевірив валідатор
        property.ChangeStatus(status);                                           // DomainException при забороненому переході
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var property = await GetTrackedAsync(id, ct);
        property.SoftDelete();
        await uow.SaveChangesAsync(ct);
    }

    public async Task AddAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
    {
        var property = await uow.Properties.GetWithDetailsAsync(propertyId, ct)
            ?? throw new EntityNotFoundException(nameof(Property), propertyId);

        if (await uow.Amenities.GetByIdAsync(amenityId, ct) is null)
            throw new EntityNotFoundException(nameof(Amenity), amenityId);

        property.AddAmenity(amenityId);          // DomainException, якщо зв'язок уже є
        await uow.SaveChangesAsync(ct);
    }

    public async Task RemoveAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
    {
        var link = await uow.PropertyAmenities.GetLinkAsync(propertyId, amenityId, ct)
            ?? throw new EntityNotFoundException("PropertyAmenity", $"{propertyId}/{amenityId}");

        uow.PropertyAmenities.Delete(link);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PropertyDto>> GetByAmenitySinceAsync(string amenityName, DateTime since, CancellationToken ct = default)
    {
        var items = await uow.PropertyAmenities.GetRecentlyAddedAsync(amenityName, since, ct);
        return mapper.Map<List<PropertyDto>>(items);
    }

    public async Task<IReadOnlyList<PropertyDto>> GetFeaturedAsync(string city, int limit, CancellationToken ct = default)
    {
        var items = await uow.Properties.ListAsync(new FeaturedPropertiesSpec(city, Math.Clamp(limit, 1, 50)), ct);
        return mapper.Map<List<PropertyDto>>(items);
    }

    private async Task<Property> GetTrackedAsync(Guid id, CancellationToken ct)
        => await uow.Properties.GetByIdAsync(id, ct)
           ?? throw new EntityNotFoundException(nameof(Property), id);
}
