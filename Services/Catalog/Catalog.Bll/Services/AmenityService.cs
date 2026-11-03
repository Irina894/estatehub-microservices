using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal.Repositories;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public class AmenityService(IUnitOfWork uow, IMapper mapper) : IAmenityService
{
    public async Task<IReadOnlyList<AmenityDto>> GetAllAsync(CancellationToken ct = default)
        => mapper.Map<List<AmenityDto>>(await uow.Amenities.GetAllAsync(ct));

    public async Task<AmenityDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var amenity = await uow.Amenities.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Amenity), id);
        return mapper.Map<AmenityDto>(amenity);
    }

    public async Task<AmenityDto> CreateAsync(CreateAmenityDto dto, CancellationToken ct = default)
    {
        var name = dto.Name.Trim();
        if (await uow.Amenities.ExistsByNameAsync(name, ct))
            throw new ConflictException($"Зручність '{name}' уже існує.");

        var amenity = new Amenity(name);
        await uow.Amenities.AddAsync(amenity, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<AmenityDto>(amenity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var amenity = await uow.Amenities.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Amenity), id);
        uow.Amenities.Delete(amenity);
        await uow.SaveChangesAsync(ct);
    }
}
